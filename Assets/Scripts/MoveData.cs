using System;

[Serializable]
public class MoveData
{
    public int turn;
    public bool player;
    public PieceType pieceType;
    public int rotation;
    public bool flipped;
    public int x;
    public int y;
    public bool touchdown;
}
