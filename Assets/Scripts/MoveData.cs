using System;

[Serializable]
public class MoveData
{
    public int turn;          // 手番
    public bool player;       // false=先手, true=後手
    public int pieceType;     // 駒の種類
    public int rotation;      // 回転
    public bool flipped;      // 反転
    public float x;           // X座標
    public float y;           // Y座標
    public bool touchdown;    // タッチダウン
}