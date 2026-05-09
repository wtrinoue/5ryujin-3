using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReplayManager : MonoBehaviour
{
    [Header("通常駒プレハブ（PieceCursor の pieces と同じ順番）")]
    public List<GameObject> piecePrefabs = new List<GameObject>();

    [Header("通常P駒の番号")]
    public int normalPNumber = 11;

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

        Debug.Log("Next：" + currentMoveIndex + "手目");
    }

    public void Prev()
    {
        if (loadedKifu == null) return;
        if (currentMoveIndex <= 0) return;

        currentMoveIndex--;
        RebuildReplay();

        Debug.Log("Prev：" + currentMoveIndex + "手目まで戻りました。");
    }

    public void ResetReplay()
    {
        currentMoveIndex = 0;
        ClearReplayObjects();

        Debug.Log("棋譜再現をリセットしました。");
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
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        replayObjects.Clear();
        startPObjects.Clear();
    }

    void PlaceMove(MoveData move)
    {
        GameObject prefab = null;

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
            if (move.pieceType < 0 || move.pieceType >= piecePrefabs.Count)
            {
                Debug.LogError("駒番号が不正です：" + move.pieceType);
                return;
            }

            prefab = piecePrefabs[move.pieceType];
        }

        if (prefab == null)
        {
            Debug.LogError("駒Prefabが設定されていません。");
            return;
        }

        Vector3 pos = new Vector3(move.x, move.y, -1f);

        // 生成
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, replayParent);

        // 回転＋反転（記録と完全一致させる）
        obj.transform.rotation = Quaternion.Euler(
            0,
            move.flipped ? 180 : 0,
            move.rotation
        );

        // 色
        Color32 pieceColor = move.player ? color2p : color1p;

        SpriteRenderer[] renderers = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in renderers)
        {
            sr.color = pieceColor;
        }

        replayObjects.Add(obj);

        // 通常P駒を記憶
        if (!move.touchdown && move.pieceType == normalPNumber)
        {
            startPObjects[move.player] = obj;
        }
    }
}