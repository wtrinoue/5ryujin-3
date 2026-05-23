# プロンプト
## PieceCursor, MagnetMap
```
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PieceCursor : MonoBehaviour
{
    public PieceType pieceType;

    [System.Serializable]
    public struct PiecePrefabPair
    {
        public PieceType type;
        public GameObject prefab;
    }

    [SerializeField] private List<PiecePrefabPair> piecesList;
    private Dictionary<PieceType, GameObject> pieces;

    private GameObject piece;
    private Stock stock;
    public static PieceCursor instance;

    public List<Transform> childMagnets = new List<Transform>();
    public List<Transform> childTiles = new List<Transform>();

    private Map mm = new Map();

    public Color32 color1p;
    public Color32 color2p;

    [Header("棋譜記録")]
    public RecordManager recordManager;

    private bool isOperatingUI = false;

    void Start()
    {
        instance = this;

        pieces = new Dictionary<PieceType, GameObject>();

        foreach (var p in piecesList)
        {
            if (p.prefab != null)
                pieces[p.type] = p.prefab;
        }
    }

    void Update()
    {
        if (Input.touchCount > 0)
            HandleTouch();
        else
            HandleMouse();
    }

    private void HandleTouch()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isOperatingUI = false;
            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return;
        }

        if (isOperatingUI) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(touch.position.x, touch.position.y, 10f)
        );

        float x = Mathf.Round(worldPos.x);
        float y = Mathf.Round(worldPos.y);

        if (touch.phase == TouchPhase.Began ||
            touch.phase == TouchPhase.Moved ||
            touch.phase == TouchPhase.Stationary)
        {
            transform.position = new Vector3(x, y, 0);
        }
    }

    private void HandleMouse()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10f)
        );

        transform.position = new Vector3(
            Mathf.Round(worldPos.x),
            Mathf.Round(worldPos.y),
            0
        );

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (piece != null && scroll != 0)
        {
            Rotate(scroll);
        }

        if (Input.GetMouseButtonDown(0)) Put();
        if (Input.GetMouseButtonDown(1)) FlipButton();
    }

    public void Rotate(float s)
    {
        if (piece != null)
            piece.transform.Rotate(0, 0, Mathf.Sign(s) * 90);
    }

    public void RotateButton()
    {
        isOperatingUI = true;
        if (piece != null) piece.transform.Rotate(0, 0, 90);
    }

    public void FlipButton()
    {
        isOperatingUI = true;
        if (piece != null) piece.transform.Rotate(0, 180, 0);
    }

    public void PutButton()
    {
        isOperatingUI = false;
        Put();
    }

    public void Put()
    {
        if (piece == null) return;

        int rotation = GetCurrentRotation();
        bool flipped = IsCurrentlyFlipped();

        int x = Mathf.RoundToInt(transform.position.x);
        int y = Mathf.RoundToInt(transform.position.y);
        bool player = Board.turn;

        bool touchdown = (pieceType == PieceType.td);

        if (mm.Add(this))
        {
            if (recordManager != null)
            {
                recordManager.AddMove(
                    pieceType: pieceType,
                    rotation: rotation,
                    flipped: flipped,
                    x: x,
                    y: y,
                    player: player,
                    touchdown: touchdown
                );

                recordManager.SaveRecord();
            }

            if (stock != null)
                stock.Decrement();

            piece.transform.SetParent(transform.parent);
            piece = null;

            Board.instance.Change();
        }
    }

    public void Trash()
    {
        if (piece != null)
        {
            Destroy(piece);
            piece = null;
        }
    }

    public void Select(PieceType type, Stock s)
    {
        stock = s;
        Trash();

        pieceType = type;

        if (!pieces.ContainsKey(type))
        {
            Debug.LogError("未登録PieceType: " + type);
            return;
        }

        MoveCursorToPointerPosition();

        piece = Instantiate(pieces[type], transform);

        childMagnets.Clear();
        childTiles.Clear();

        for (int i = 0; i < piece.transform.childCount; i++)
        {
            Transform child = piece.transform.GetChild(i);

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.sortingOrder = 10;
                sr.color = Board.turn ? color2p : color1p;
            }

            if (child.CompareTag("Magnet"))
                childMagnets.Add(child);
            else if (child.CompareTag("Tile"))
                childTiles.Add(child);
        }
    }

    private void MoveCursorToPointerPosition()
    {
        Vector3 screenPos = Input.touchCount > 0
            ? (Vector3)Input.GetTouch(0).position
            : Input.mousePosition;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 10f)
        );

        transform.position = new Vector3(
            Mathf.Round(worldPos.x),
            Mathf.Round(worldPos.y),
            0
        );
    }

    private int GetCurrentRotation()
    {
        float z = piece.transform.eulerAngles.z;
        int rot = Mathf.RoundToInt(z) % 360;
        if (rot < 0) rot += 360;

        return (Mathf.RoundToInt(rot / 90f) * 90) % 360;
    }

    private bool IsCurrentlyFlipped()
    {
        float y = piece.transform.eulerAngles.y;
        int ry = Mathf.RoundToInt(y) % 360;
        if (ry < 0) ry += 360;

        return ry == 180;
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

public class Map
{

    Dictionary<(float x, float y), (bool player, int num)> magnetMap;
    Dictionary<(float x, float y), bool> placedTiles;
    Dictionary<bool, int> dragonCount;
    public static Dictionary<(bool, int), (GameObject go, List<(float x, float y)> pos)>pdict;
    public static Dictionary<bool, int>reach;
    public Map()
    {
        pdict = new Dictionary<(bool, int), (GameObject go, List<(float x, float y)> pos)>();
        reach = new Dictionary<bool, int> {{false,0},{true,29}};
        this.magnetMap = new Dictionary<(float x, float y), (bool player, int num)>();
        this.placedTiles = new Dictionary<(float x, float y), bool>();
        dragonCount = new Dictionary<bool, int> { { false, 0 }, { true, 0 } };
    }
    (float x, float y) ToTuple(Vector3 v)
    {
        return (Mathf.Round(v.x*2)/2,Mathf.Round(v.y*2)/2);
    }
    public bool Add(PieceCursor piece)
    {
        bool player = Board.turn;
        int num = 0;
        //Debug.Log(piece.childTiles.Count);
        int neighbor = 0;
        bool first = false;
        for (int i = 0; i < 5; i++)
        {
            var t = piece.childTiles[i];

            var tp = ToTuple(t.transform.position);
            if (placedTiles.ContainsKey(tp))
            {
                return false;
            }
            if (tp.x < 0 || tp.x >= 60 || tp.y < 0 || tp.y >= 30)
            {
                return false;
            }
            foreach ((float x, float y) d in new (float, float)[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
            {
                var p = (tp.x + d.x, tp.y + d.y);
                if (placedTiles.ContainsKey(p))
                {
                    if (placedTiles[p] == Board.turn)
                    {
                        neighbor++;
                    }

                }
            }
        }
        
        
        for (int i = 0; i < piece.childMagnets.Count; i++)
        {
            if (piece.pieceType == PieceType.P)
            {
                var p = piece.transform.GetChild(0).Find("Pointer");
                var y = p.transform.position.y;
                y = Mathf.Round(y * 2) / 2;
                if (y == 0.5 && !Board.turn || y == 28.5 && Board.turn)
                {
                    var tps = new List<(float, float)>();
                    for (int j = 0; j < 5; j++)
                    {
                        var c = piece.childTiles[j];
                        var tp = ToTuple(c.transform.position);
                        tps.Add(tp);
                    }
                    num = dragonCount[Board.turn];
                    dragonCount[Board.turn] += 1;
                    first = true;
                    pdict.Add((Board.turn, num), (piece.transform.GetChild(0).gameObject,tps));
                    break;
                }
                else
                {
                    return false;
                }
                // bool a = false;
                // for (int j = 0; j < piece.childTiles.Count; j++)
                // {
                //     Debug.Log(piece.childTiles[j].transform.position.y);
                //     var y = piece.childTiles[j].transform.position.y;
                //     if (y == 0&&!Board.turn||y == 29&&Board.turn)
                //     {
                //         a = true;
                //         break;
                //     }
                // }
                // if (a)
                // {
                //     break;
                // }
            }
            var m = piece.childMagnets[i];
            var t = ToTuple(m.transform.position);
            if (magnetMap.ContainsKey(t))
            {
                if (player == magnetMap[t].player)
                {
                    //player = magnetMap[t].player;
                    num = magnetMap[t].num;
                    break;
                }

            }
            if (i == piece.childMagnets.Count - 1)
            {
                return false;
            }
        }
        if (neighbor != 1 && !first || neighbor != 0 && first)
        {
            return false;
        }
        int r;
        if (Board.turn)
        {
            r = 29;
        }
        else
        {
            r = 0;
        }
        for (int i = 0; i < 5; i++)
        {
            var t = piece.childTiles[i];
            var tp = ToTuple(t.transform.position);
            this.placedTiles.Add(tp, Board.turn);
            if (piece.pieceType == PieceType.td)
            {
                if (Board.turn)
                {
                    r = (int)Mathf.Min(r, tp.y);
                    reach[true] = (int)Mathf.Min(r, reach[true]);
                }
                else
                {
                    r = (int)Mathf.Max(r, tp.y);
                    reach[false] = (int)Mathf.Max(r, reach[false]);
                }
            }
            
        }
        
        var keys = GetKeysFromValue(magnetMap, (player, num));

        foreach (var key in keys)
        {
            magnetMap.Remove(key);
        }
        for (int i = 0; i < piece.childMagnets.Count; i++)
        {
            
            var m = piece.childMagnets[i];
            var t = ToTuple(m.transform.position);
            if (magnetMap.ContainsKey(t)||piece.pieceType == PieceType.td)
            {

            }
            else
            {
                magnetMap.Add(t, (player, num));
            }
        }
        if (piece.pieceType == PieceType.td)
        {
            //Debug.Log(0);
            var p = pdict[(Board.turn, num)];
            p.go.SetActive(false);
            foreach (var item in p.pos)
            {
                //Debug.Log(item);
                placedTiles.Remove(item);
            }
            if (Board.turn)
            {
                if (r == 0)
                {
                    Board.instance.Win(2);
                }

            }
            else
            {
                if (r == 29)
                {
                    Board.instance.Win(1);
                }

            }
        }
        return true;
    

    }
    static List<(float x, float y)> GetKeysFromValue(Dictionary<(float x, float y), (bool player, int num)> dictionary, (bool player, int num) value)
    {
        return dictionary.Where(kvp => kvp.Value == value).Select(kvp => kvp.Key).ToList();
    }
}

```