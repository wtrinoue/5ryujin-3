using System.Collections.Generic;
using UnityEngine;

public class PieceInfo
{
    public PieceData pieceData;
    public List<Vector2Int> childTiles;
    public List<Vector2> childMagnets;

    public PieceInfo(PieceData pd)
    {
        pieceData = pd;
    }

    // =========================
    // 基本変換（純粋関数）
    // =========================

    private Vector2 Flip(Vector2 v)
    {
        return new Vector2(-v.x, v.y);
    }

    private Vector2 Rotate(Vector2 v, int rotation)
    {
        switch (rotation)
        {
            case 0:
                return v;
            case 90:
                return new Vector2(-v.y, v.x);
            case 180:
                return new Vector2(-v.x, -v.y);
            case 270:
                return new Vector2(v.y, -v.x);
            default:
                return v;
        }
    }

    private Vector2 Transform(Vector2 v, MoveData md)
    {
        // 反転 → 回転
        if (md.flipped)
            v = Flip(v);

        v = Rotate(v, md.rotation);

        return v;
    }

    // =========================
    // Tiles（盤面占有セル）
    // =========================

    public List<Vector2Int> GetChildTiles(MoveData md)
    {
        List<Vector2Int> result = new();

        foreach (var t in pieceData.tiles)
        {
            Vector2 v = new Vector2(t.x, t.y);

            v = Transform(v, md);

            result.Add(new Vector2Int(
                Mathf.RoundToInt(v.x) + md.x,
                Mathf.RoundToInt(v.y) + md.y
            ));
        }

        return result;
    }

    // =========================
    // Magnets（補助ポイント）
    // =========================

    public List<Vector2> GetChildMagnets(MoveData md)
    {
        List<Vector2> result = new();

        foreach (var m in pieceData.magnets)
        {
            Vector2 v = Transform(m, md);

            result.Add(new Vector2(
                v.x + md.x,
                v.y + md.y
            ));
        }

        return result;
    }
    // =========================
    // Pointer（補助ポイント）
    // =========================
    public Vector2 GetPointer(MoveData md)
    {
        // カーソル基準のローカル座標
        Vector2 localPointer = new Vector2(0.5f, 0.5f);

        // flip → rotate
        Vector2 transformed = Transform(localPointer, md);

        // ワールド座標へ
        return new Vector2(
            transformed.x + md.x,
            transformed.y + md.y
        );
    }

    // =========================
    // 一括取得（便利用）
    // =========================

    public (List<Vector2Int> tiles, List<Vector2> magnets) GetAll(MoveData md)
    {
        return (GetChildTiles(md), GetChildMagnets(md));
    }

    public void TestDebug(MoveData md)
    {
        var tiles = GetChildTiles(md);
        var magnets = GetChildMagnets(md);
        var pointer = GetPointer(md);

        string log = "===== PieceInfo Debug =====\n";
        log += $"PieceType: {pieceData.pieceType}\n";
        log += $"MoveData: pos({md.x},{md.y}) rot({md.rotation}) flipped({md.flipped})\n";

        log += "--- Tiles ---\n";
        for (int i = 0; i < tiles.Count; i++)
        {
            log += $"Tile[{i}] = {tiles[i]}\n";
        }

        log += "--- Magnets ---\n";
        for (int i = 0; i < magnets.Count; i++)
        {
            log += $"Magnet[{i}] = {magnets[i]}\n";
        }

        log += "--- Pointer ---\n";
        log += $"Pointer = {pointer}\n";

        log += "===========================";

        Debug.Log(log);
    }
}