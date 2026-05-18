using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayManager : MonoBehaviour
{
    [Header("PieceType → Prefab対応表")]
    [SerializeField] private List<PiecePrefabPair> piecePrefabs = new List<PiecePrefabPair>();

    [System.Serializable]
    public struct PiecePrefabPair
    {
        public PieceType type;
        public GameObject prefab;
    }

    private Dictionary<PieceType, GameObject> prefabDict;

    [Header("タッチダウン用P駒")]
    public GameObject touchdownPrefab;

    [Header("生成した駒を入れる親")]
    public Transform replayParent;

    [Header("駒の色")]
    public Color32 color1p;
    public Color32 color2p;

    private GameRecord loadedKifu;
    private List<GameObject> replayObjects = new List<GameObject>();
    private Dictionary<bool, GameObject> startPObjects = new Dictionary<bool, GameObject>();

    private int currentMoveIndex = 0;

    void Start()
    {
        // ★辞書化
        prefabDict = new Dictionary<PieceType, GameObject>();

        foreach (var p in piecePrefabs)
        {
            if (p.prefab != null)
            {
                prefabDict[p.type] = p.prefab;
            }
            else
            {
                Debug.LogWarning($"Prefab未設定: {p.type}");
            }
        }

        if (KifuReplayContext.HasKifu())
        {
            loadedKifu = KifuReplayContext.selectedKifu;
            Debug.Log("棋譜を受け取りました。手数：" + loadedKifu.moves.Count);
            ResetReplay();
        }
        else
        {
            Debug.LogWarning("再現する棋譜がありません。HOME画面から棋譜を選んでください。");
        }
    }

    public void Next()
    {
        if (loadedKifu == null) return;
        if (currentMoveIndex >= loadedKifu.moves.Count) return;

        currentMoveIndex++;
        RebuildReplay();
    }

    public void Prev()
    {
        if (loadedKifu == null) return;
        if (currentMoveIndex <= 0) return;

        currentMoveIndex--;
        RebuildReplay();
    }

    public void ResetReplay()
    {
        currentMoveIndex = 0;
        ClearReplayObjects();
    }

    public void GoHome()
    {
        ResetReplay();
        KifuReplayContext.Clear();
        SceneManager.LoadScene("HomeScene");
    }

    void RebuildReplay()
    {
        ClearReplayObjects();

        for (int i = 0; i < currentMoveIndex; i++)
        {
            PlaceMove(loadedKifu.moves[i]);
        }
    }

    void ClearReplayObjects()
    {
        foreach (GameObject obj in replayObjects)
        {
            if (obj != null) Destroy(obj);
        }

        replayObjects.Clear();
        startPObjects.Clear();
    }

    void PlaceMove(MoveData move)
    {
        GameObject prefab = null;

        // =========================
        // タッチダウン
        // =========================
        if (move.touchdown)
        {
            if (startPObjects.ContainsKey(move.player))
            {
                GameObject startP = startPObjects[move.player];

                if (startP != null)
                {
                    replayObjects.Remove(startP);
                    Destroy(startP);
                }

                startPObjects.Remove(move.player);
            }

            prefab = touchdownPrefab;
        }
        else
        {
            PieceType type = move.pieceType;

            if (!prefabDict.TryGetValue(type, out prefab))
            {
                Debug.LogError("未登録PieceType: " + type);
                return;
            }
        }

        if (prefab == null)
        {
            Debug.LogError("Prefabがnullです");
            return;
        }

        Vector3 pos = new Vector3(move.x, move.y, -1f);

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, replayParent);

        obj.transform.rotation = Quaternion.Euler(
            0,
            move.flipped ? 180 : 0,
            move.rotation
        );

        Color32 pieceColor = move.player ? color2p : color1p;

        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
        {
            sr.color = pieceColor;
        }

        replayObjects.Add(obj);

        // P駒管理（必要なら残す）
        if (!move.touchdown && move.pieceType == PieceType.P)
        {
            startPObjects[move.player] = obj;
        }
    }
}
