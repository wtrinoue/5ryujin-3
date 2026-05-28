using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;

public class MdMap
{
    Dictionary<(float x, float y), (bool player, int num)> magnetMap;
    Dictionary<(float x, float y), bool> placedTiles;
    Dictionary<bool, int> dragonCount;
    public static Dictionary<(bool, int), (GameObject go, List<(float x, float y)> pos)> pdict;
    public static Dictionary<bool, int> reach;
    public PieceDatabase pieceDatabase;
    public MdMap(PieceDatabase pieceDatabase)
    {
        this.pieceDatabase = pieceDatabase;
        pdict = new Dictionary<(bool, int), (GameObject go, List<(float x, float y)> pos)>();
        reach = new Dictionary<bool, int> { { false, 0 }, { true, 29 } };
        this.magnetMap = new Dictionary<(float x, float y), (bool player, int num)>();
        this.placedTiles = new Dictionary<(float x, float y), bool>();
        dragonCount = new Dictionary<bool, int> { { false, 0 }, { true, 0 } };
    }
    (float x, float y) ToTuple(Vector3 v)
    {
        return (Mathf.Round(v.x * 2) / 2, Mathf.Round(v.y * 2) / 2);
    }
    /// <summary>
    /// 指定されたMoveDataが配置可能かどうかを判定します（副作用なし）。
    /// </summary>
    public bool CanAdd(MoveData md)
    {
        PieceData pd = pieceDatabase.Get(md.pieceType);
        PieceInfo info = new PieceInfo(pd);
        List<(float x, float y)> childTiles =
            info.GetChildTiles(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));
        List<(float x, float y)> childMagnets =
            info.GetChildMagnets(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));
        bool player = md.player;
        int neighbor = 0;
        bool first = false;

        // 1. タイルの重複・枠外チェック、および隣接タイルのカウント
        for (int i = 0; i < childTiles.Count; i++)
        {
            (float x, float y) tp = childTiles[i];
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
                (float x, float y) p = (tp.x + d.x, tp.y + d.y);
                if (placedTiles.ContainsKey(p))
                {
                    if (placedTiles[p] == player)
                    {
                        neighbor++;
                    }
                }
            }
        }

        // 2. マグネット・特殊ゴマ（Pゴマ）のルールチェック
        for (int i = 0; i < childMagnets.Count; i++)
        {
            if (md.pieceType == PieceType.P)
            {
                Vector2 v = info.GetPointer(md);
                (float x, float y) p = (v.x, v.y);
                float y = p.y;
                y = Mathf.Round(y * 2) / 2;
                if (y == 0.5 && !player || y == 28.5 && player)
                {
                    first = true;
                    break;
                }
                else
                {
                    return false;
                }
            }
            (float x, float y) t = childMagnets[i];
            if (magnetMap.ContainsKey(t))
            {
                if (player == magnetMap[t].player)
                {
                    break;
                }
            }
            if (i == childMagnets.Count - 1)
            {
                return false;
            }
        }

        // 3. 隣接条件のチェック
        if (neighbor != 1 && !first || neighbor != 0 && first)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 判定が通った前提で、状態の更新処理（追加・削除・勝利判定など）を行います。
    /// </summary>
    public void ApplyAdd(MoveData md)
    {
        PieceData pd = pieceDatabase.Get(md.pieceType);
        PieceInfo info = new PieceInfo(pd);
        List<(float x, float y)> childTiles =
            info.GetChildTiles(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));
        List<(float x, float y)> childMagnets =
            info.GetChildMagnets(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));
        bool player = md.player;

        // 安全にマグネットID (num) を取得
        int num = GetMagnetNum(childMagnets, player, md.pieceType);
        int r = player ? 29 : 0;

        // 1. タイルの配置と reach の計算
        for (int i = 0; i < childTiles.Count; i++)
        {
            (float x, float y) tp = childTiles[i];
            this.placedTiles.Add(tp, player);
            if (md.pieceType == PieceType.td)
            {
                if (player)
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

        // 2. 既存の該当マグネットマップの削除
        var keys = GetKeysFromValue(magnetMap, (player, num));
        foreach (var key in keys)
        {
            magnetMap.Remove(key);
        }

        // 3. 新しいマグネットの追加
        for (int i = 0; i < childMagnets.Count; i++)
        {
            (float x, float y) t = childMagnets[i];
            if (magnetMap.ContainsKey(t) || md.pieceType == PieceType.td)
            {
                // スキップ
            }
            else
            {
                magnetMap.Add(t, (player, num));
            }
        }

        // 4. td（特定ゴマ）の場合の特殊処理、および勝敗判定
        if (md.pieceType == PieceType.td)
        {
            var p = pdict[(player, num)];
            p.go.SetActive(false);
            foreach (var item in p.pos)
            {
                placedTiles.Remove(item);
            }
            if (player)
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
    }

    // ApplyAddの内部でnumを特定するためのプライベート補助関数
    private int GetMagnetNum(List<(float x, float y)> childMagnets, bool player, PieceType pieceType)
    {
        if (pieceType == PieceType.P) return 0;

        for (int i = 0; i < childMagnets.Count; i++)
        {
            (float x, float y) t = childMagnets[i];
            if (magnetMap.ContainsKey(t))
            {
                if (player == magnetMap[t].player)
                {
                    return magnetMap[t].num;
                }
            }
        }
        return 0;
    }
    public bool Add(MoveData md)
    {
        PieceData pd = pieceDatabase.Get(md.pieceType);
        PieceInfo info = new PieceInfo(pd);
        List<(float x, float y)> childTiles =
            info.GetChildTiles(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));
        List<(float x, float y)> childMagnets =
            info.GetChildMagnets(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));
        bool player = md.player; // $MoveDataから持っていける。
        int num = 0;
        //Debug.Log(piece.childTiles.Count);
        int neighbor = 0;
        bool first = false;
        for (int i = 0; i < childTiles.Count; i++)
        {
            (float x, float y) t = childTiles[i];

            (float x, float y) tp = (t.x, t.y);
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
                (float x, float y) p = (tp.x + d.x, tp.y + d.y);
                if (placedTiles.ContainsKey(p))
                {
                    if (placedTiles[p] == player)
                    {
                        neighbor++;
                    }

                }
            }
        }


        for (int i = 0; i < childMagnets.Count; i++)
        {
            if (md.pieceType == PieceType.P)
            {
                Debug.Log("Pゴマがおかれました");
                Vector2 v = info.GetPointer(md);
                (float x, float y) p = (v.x, v.y);
                float y = p.y;
                y = Mathf.Round(y * 2) / 2;
                if (y == 0.5 && !player || y == 28.5 && player)
                {
                    List<(float, float)> tps = new();
                    for (int j = 0; j < childTiles.Count; j++)
                    {
                        (float x, float y) c = childTiles[j];
                        tps.Add(c);
                    }
                    first = true;
                    break;
                }
                else
                {
                    Debug.Log("ダメでした");
                    return false;
                }
            }
            (float x, float y) t = childMagnets[i];
            if (magnetMap.ContainsKey(t))
            {
                if (player == magnetMap[t].player)
                {
                    //player = magnetMap[t].player;
                    num = magnetMap[t].num;
                    break;
                }

            }
            if (i == childMagnets.Count - 1)
            {
                return false;
            }
        }
        if (neighbor != 1 && !first || neighbor != 0 && first)
        {
            return false;
        }
        int r;
        if (player)
        {
            r = 29;
        }
        else
        {
            r = 0;
        }
        for (int i = 0; i < childTiles.Count; i++)
        {
            (float x, float y) tp = childTiles[i];
            this.placedTiles.Add(tp, player);
            if (md.pieceType == PieceType.td)
            {
                if (player)
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
        for (int i = 0; i < childMagnets.Count; i++)
        {
            (float x, float y) t = childMagnets[i];
            if (magnetMap.ContainsKey(t) || md.pieceType == PieceType.td)
            {

            }
            else
            {
                magnetMap.Add(t, (player, num));
            }
        }
        if (md.pieceType == PieceType.td)
        {
            //Debug.Log(0);
            var p = pdict[(player, num)];
            p.go.SetActive(false);
            foreach (var item in p.pos)
            {
                //Debug.Log(item);
                placedTiles.Remove(item);
            }
            if (player)
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
    public void AddPDict(MoveData md, GameObject go)
    {
        PieceData pd = pieceDatabase.Get(md.pieceType);
        PieceInfo info = new PieceInfo(pd);

        List<(float x, float y)> childTiles =
            info.GetChildTiles(md)
                .ConvertAll(v => ((float)v.x, (float)v.y));

        // IDは内部で発行
        bool player = md.player;

        int num = dragonCount[player];
        dragonCount[player]++;

        pdict[(player, num)] = (go, childTiles);
        DebugPrintPDict();
    }
    public void Reset()
    {
        this.pieceDatabase = pieceDatabase;
        pdict = new Dictionary<(bool, int), (GameObject go, List<(float x, float y)> pos)>();
        reach = new Dictionary<bool, int> { { false, 0 }, { true, 29 } };
        this.magnetMap = new Dictionary<(float x, float y), (bool player, int num)>();
        this.placedTiles = new Dictionary<(float x, float y), bool>();
        dragonCount = new Dictionary<bool, int> { { false, 0 }, { true, 0 } };
    }

    public void DebugPrintPDict()
    {
        if (pdict == null)
        {
            Debug.Log("pdict is NULL");
            return;
        }

        string log = "===== PDICT DUMP =====\n";
        log += $"Count = {pdict.Count}\n";

        foreach (var kvp in pdict)
        {
            var key = kvp.Key;
            var value = kvp.Value;

            string goName = value.go != null ? value.go.name : "NULL";
            int posCount = value.pos != null ? value.pos.Count : -1;

            log += $"key={key} | go={goName} | posCount={posCount}\n";
        }

        log += "===== END =====";

        Debug.Log(log);
    }
    static List<(float x, float y)> GetKeysFromValue(Dictionary<(float x, float y), (bool player, int num)> dictionary, (bool player, int num) value)
    {
        return dictionary.Where(kvp => kvp.Value == value).Select(kvp => kvp.Key).ToList();
    }
}