using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class PieceDatabase : MonoBehaviour
{
    public List<PieceData> pieces;

    private Dictionary<PieceType, PieceData> dict;

    void Awake()
    {
        dict = pieces.ToDictionary(p => p.pieceType);
    }

    public PieceData Get(PieceType type)
    {
        return dict[type];
    }

    public static PieceDatabase Instance;
}