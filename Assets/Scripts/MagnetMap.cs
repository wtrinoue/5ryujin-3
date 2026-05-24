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
    public static Dictionary<(bool, int), (GameObject go, List<(float x, float y)> pos)> pdict;
    public static Dictionary<bool, int> reach;
    public PieceDatabase pieceDatabase;
    public bool pdicAddable = false;
    public Map(PieceDatabase pieceDatabase)
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
    public bool Add(PieceCursor piece)
    {
        bool player = Board.turn; // $MoveDataから持っていける。
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
                    pdict.Add((Board.turn, num), (piece.transform.GetChild(0).gameObject, tps));
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
            if (magnetMap.ContainsKey(t) || piece.pieceType == PieceType.td)
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

    public bool AddFromMd(MoveData md)
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
