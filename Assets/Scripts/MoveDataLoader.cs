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

    private Dictionary<PieceType, GameObject> pieceDict;
    private MdMap mm;

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
        // ストック減少処理（そのまま）
        Board.instance.DecrementStock(md.player, md.pieceType);

        // 生成位置（そのまま）
        Vector3 pos = new Vector3(md.x, 0f, md.y);

        // プレハブ取得（ロジック維持）
        if (!pieceDict.TryGetValue(md.pieceType, out GameObject prefab))
        {
            Debug.LogError($"Prefab not found for {md.pieceType}");
            return;
        }

        // 生成（そのまま）
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);

        // 回転（そのまま）
        obj.transform.rotation = Quaternion.Euler(0f, md.rotation * 90f, 0f);

        // フリップ（そのまま）
        if (md.flipped)
        {
            Vector3 scale = obj.transform.localScale;
            scale.x *= -1;
            obj.transform.localScale = scale;
        }

        // マップ登録（そのまま）
        mm.Add(md);
    }
    public void LoadMoveDataList(List<MoveData> moveList)
    {
        for (int i = 0; i < moveList.Count; i++)
        {
            LoadMoveData(moveList[i]);
        }
    }
}