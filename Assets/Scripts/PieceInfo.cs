using System.Collections.Generic;
using UnityEngine;
public class PieceInfo
{
    public PieceType pieceType;
    public List<Vector2Int> childTiles;
    public List<Vector2> childMagnets;

    public PieceInfo(MoveData md)
    {

    }
}

/*
Mapをできるだけ、PieceCursorを用いたときの書き方から変更せずに、MoveDataを用いた判定の仕方で済むようにする。
*/