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

    private MdMap mm;

    public Color32 color1p;
    public Color32 color2p;

    [Header("棋譜記録")]
    public RecordManager recordManager;
    [Header("PieceDatabase")]
    public PieceDatabase pieceDatabase;

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

        mm = new MdMap(pieceDatabase);
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
        // =========================
        // ここがMoveData生成部分
        // =========================
        MoveData md = new MoveData
        {
            turn = 0, // 必要ならRecordManager側で管理してもOK
            player = player,
            pieceType = pieceType,
            rotation = rotation,
            flipped = flipped,
            x = x,
            y = y,
            touchdown = touchdown
        };
        // =========================
        // ここがMoveData生成部分
        // =========================
        PieceInfo info = new PieceInfo(pieceDatabase.Get(pieceType));
        info.TestDebug(md);
        TestDebugCursor();
        // Debug.Log($"mm.Add(this) = {mm.Add(this)}");
        // Debug.Log($"mm.AddFromMd(md) = {mm.AddFromMd(md)}");
        if (mm.Add(md))
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
            // pdictへの追加
            if (pieceType == PieceType.P)
            {
                Debug.Log("Pゴマを追加しました");
                mm.AddPDict(md, piece);
            }
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

    public void TestDebugCursor()
    {
        string log = "===== PieceCursor Debug =====\n";
        log += $"PieceType: {pieceType}\n";
        log += $"Position: ({transform.position.x},{transform.position.y})\n";
        log += $"RotationZ: {piece.transform.eulerAngles.z}\n";
        log += $"FlippedY: {piece.transform.eulerAngles.y}\n";

        log += "--- ChildTiles (Transform) ---\n";
        for (int i = 0; i < childTiles.Count; i++)
        {
            Vector3 p = childTiles[i].position;
            log += $"Tile[{i}] = ({Mathf.RoundToInt(p.x)}, {Mathf.RoundToInt(p.y)})\n";
        }

        log += "--- ChildMagnets (Transform) ---\n";
        for (int i = 0; i < childMagnets.Count; i++)
        {
            Vector3 p = childMagnets[i].position;
            log += $"Magnet[{i}] = ({p.x:F2}, {p.y:F2})\n";
        }

        log += "==============================";

        Debug.Log(log);
    }
}
