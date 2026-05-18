using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PieceData", menuName = "Ryujin/PieceData")]
public class PieceData : ScriptableObject
{
    public PieceType pieceType;
    public List<Vector2Int> tiles;
    public List<Vector2> magnets;
}

public enum PieceType
{
    F,
    I,
    L,
    N,
    P,
    T,
    td,
    U,
    V,
    W,
    X,
    Y,
    Z
}
