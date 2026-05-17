using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PieceCursor : MonoBehaviour
{
    public int number; // 0～12
    [SerializeField] private List<GameObject> pieces;

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

    // 追加：スマホでUI操作中は駒を追従させない
    private bool isOperatingUI = false;

    void Start()
    {
        instance = this;
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            HandleTouch();
        }
        else
        {
            HandleMouse();
        }
    }

    // =========================
    // スマホ操作
    // =========================
    private void HandleTouch()
    {
        Touch touch = Input.GetTouch(0);

        // 指を離したら、UI操作中フラグを解除
        if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            isOperatingUI = false;
            return;
        }

        // UIボタン上のタッチなら、駒を追従させない
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return;
        }

        // 回転・反転ボタン操作中は、その場に留める
        if (isOperatingUI)
        {
            return;
        }

        Vector3 screenPos = touch.position;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 10f)
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

    // =========================
    // PC操作
    // =========================
    private void HandleMouse()
    {
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, 10f)
        );

        float x = Mathf.Round(worldPos.x);
        float y = Mathf.Round(worldPos.y);

        transform.position = new Vector3(x, y, 0);

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (piece != null && scroll != 0)
        {
            Rotate(scroll);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Put();
        }

        if (Input.GetMouseButtonDown(1))
        {
            FlipButton();
        }
    }

    // =========================
    // 選択時にポインタ位置へ移動
    // =========================
    private void MoveCursorToPointerPosition()
    {
        Vector3 screenPos;

        if (Input.touchCount > 0)
        {
            screenPos = Input.GetTouch(0).position;
        }
        else
        {
            screenPos = Input.mousePosition;
        }

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(screenPos.x, screenPos.y, 10f)
        );

        float x = Mathf.Round(worldPos.x);
        float y = Mathf.Round(worldPos.y);

        transform.position = new Vector3(x, y, 0);
    }

    // =========================
    // PC用回転
    // =========================
    public void Rotate(float s)
    {
        if (piece != null)
        {
            piece.transform.Rotate(0, 0, Mathf.Sign(s) * 90);
        }
    }

    // =========================
    // UIボタン用
    // =========================
    public void RotateButton()
    {
        isOperatingUI = true;

        if (piece != null)
        {
            piece.transform.Rotate(0, 0, 90);
        }
    }

    public void FlipButton()
    {
        isOperatingUI = true;

        if (piece != null)
        {
            piece.transform.Rotate(0, 180, 0);
        }
    }

    public void PutButton()
    {
        isOperatingUI = false;
        Put();
    }

    // =========================
    // 配置処理
    // =========================
    public void Put()
    {
        if (piece != null)
        {
            int rotation = GetCurrentRotation();
            bool flipped = IsCurrentlyFlipped();
            int x =  Mathf.RoundToInt(transform.position.x);
            int y = Mathf.RoundToInt(transform.position.y);
            bool player = Board.turn;

            bool touchdown = (number == 12);

            if (mm.Add(this))
            {
                if (recordManager != null)
                {
                    recordManager.AddMove(
                        pieceType: number,
                        rotation: rotation,
                        flipped: flipped,
                        x: x,
                        y: y,
                        player: player,
                        touchdown: touchdown
                    );

                    recordManager.SaveRecord();
                }
                else
                {
                    Debug.LogWarning("RecordManager が設定されていません");
                }

                stock.Decrement();
                piece.transform.SetParent(transform.parent);
                piece = null;
                Board.instance.Change();
            }
        }
    }

    // =========================
    // 駒削除
    // =========================
    public void Trash()
    {
        if (piece != null)
        {
            Destroy(piece);
            piece = null;
        }
    }

    // =========================
    // 駒選択
    // =========================
    public void Select(int n, Stock s)
    {
        stock = s;
        Trash();

        number = n;

        if (number < 0 || number >= pieces.Count)
        {
            Debug.LogError("pieces の範囲外です: number=" + number + " / pieces.Count=" + pieces.Count);
            return;
        }

        MoveCursorToPointerPosition();

        piece = Instantiate(pieces[number], transform);

        childMagnets = new List<Transform>();
        childTiles = new List<Transform>();

        for (int i = 0; i < piece.transform.childCount; i++)
        {
            Transform child = piece.transform.GetChild(i);

            if (child.CompareTag("Magnet"))
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 10;
                    sr.color = Board.turn ? color2p : color1p;
                }
                childMagnets.Add(child);
            }
            else if (child.CompareTag("Tile"))
            {
                SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingOrder = 10;
                    sr.color = Board.turn ? color2p : color1p;
                }
                childTiles.Add(child);
            }
        }
    }

    // =========================
    // 現在の回転角を取得
    // =========================
    private int GetCurrentRotation()
    {
        if (piece == null) return 0;

        float z = piece.transform.eulerAngles.z;
        int rot = Mathf.RoundToInt(z) % 360;

        if (rot < 0) rot += 360;

        rot = Mathf.RoundToInt(rot / 90f) * 90;
        rot %= 360;

        return rot;
    }

    // =========================
    // 現在の反転状態を取得
    // =========================
    private bool IsCurrentlyFlipped()
    {
        if (piece == null) return false;

        float y = piece.transform.eulerAngles.y;
        int ry = Mathf.RoundToInt(y) % 360;
        if (ry < 0) ry += 360;

        return ry == 180;
    }
}