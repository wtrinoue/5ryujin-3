using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveDataLoader : MonoBehaviour
{
    [Header("PieceDatabase")]
    public PieceDatabase pieceDatabase;

    [Header("Piece Prefabs (Inspector)")]
    [SerializeField]
    private List<PieceEntry> pieceEntries;
    [Header("PieceContainer")]
    public GameObject pieceContainer;

    private Dictionary<PieceType, GameObject> pieceDict;
    public MdMap mm;

    [System.Serializable]
    public class PieceEntry
    {
        public PieceType type;
        public GameObject prefab;
    }

    void Awake()
    {
        pieceDict = new Dictionary<PieceType, GameObject>();

        foreach (var e in pieceEntries)
        {
            if (!pieceDict.ContainsKey(e.type))
            {
                pieceDict.Add(e.type, e.prefab);
            }
            else
            {
                Debug.LogWarning($"Duplicate PieceType: {e.type}");
            }
        }
    }

    void Start()
    {
        mm = new MdMap(pieceDatabase);
    }

    public void LoadMoveData(MoveData md)
    {
        // ストック減少処理
        Board.instance.DecrementStock(md.player, md.pieceType);

        // プレハブ取得
        if (!pieceDict.TryGetValue(md.pieceType, out GameObject prefab))
        {
            Debug.LogError($"Prefab not found for {md.pieceType}");
            return;
        }

        // 生成位置（MoveDataの中心）
        Vector3 pos = new Vector3(md.x, md.y, 0f);

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, pieceContainer.transform);

        // =========================
        // rotation（基本回転）
        // =========================
        obj.transform.rotation = Quaternion.Euler(0f, 0f, md.rotation);

        // =========================
        // flipped（Z回転ベースで補正）
        // =========================
        if (md.flipped)
        {
            // 「piece.transform.Rotate(0, 0, 90) を参考」
            // → Z軸で90度追加回転として扱う
            obj.transform.Rotate(0f, 180f, 0f);
        }
        // =========================
        // ★追加：プレイヤーによる色変更
        // =========================
        Color c = md.player ? Color.black : Color.red;
        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.color = c;
        }
        // マップ登録
        mm.ApplyAdd(md);
    }
    public void LoadMoveDataList(List<MoveData> moveList)
    {
        for (int i = 0; i < moveList.Count; i++)
        {
            LoadMoveData(moveList[i]);
        }
    }

    public void Reset()
    {
        // MdMapリセット
        mm.Reset();
        // Stockリセット
        Board.instance.ResetStock();
        // 古いContainer削除
        if (pieceContainer != null)
        {
            Destroy(pieceContainer);
        }

        // 新しいEmpty生成
        pieceContainer = new GameObject("PieceContainer");
    }

    public void TestLoad()
    {
        List<MoveData> mdList = new List<MoveData>
        {
            new MoveData
            {
                turn = 0,
                player = false,
                pieceType = PieceType.P,
                rotation = 180,
                flipped = false,
                x = 26,
                y = 1,
                touchdown = false
            },
            new MoveData
            {
                turn = 0,
                player = true,
                pieceType = PieceType.P,
                rotation = 0,
                flipped = false,
                x = 14,
                y = 28,
                touchdown = false
            },
            new MoveData
            {
                turn = 0,
                player = false,
                pieceType = PieceType.W,
                rotation = 270,
                flipped = false,
                x = 27,
                y = 4,
                touchdown = false
            },
            new MoveData
            {
                turn = 0,
                player = true,
                pieceType = PieceType.V,
                rotation = 0,
                flipped = false,
                x = 15,
                y = 25,
                touchdown = false
            },
            new MoveData
            {
                turn = 0,
                player = false,
                pieceType = PieceType.V,
                rotation = 90,
                flipped = false,
                x = 30,
                y = 6,
                touchdown = false
            },
            new MoveData
            {
                turn = 0,
                player = true,
                pieceType = PieceType.V,
                rotation = 180,
                flipped = false,
                x = 18,
                y = 23,
                touchdown = false
            }
        };

        LoadMoveDataList(mdList);
    }
}