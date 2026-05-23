using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PieceDatabase", menuName = "Ryujin/PieceDatabase")]
public class PieceDatabase : ScriptableObject
{
    [SerializeField]
    private List<PieceData> pieces;

    private Dictionary<PieceType, PieceData> dict;

    private void OnEnable()
    {
        dict = pieces.ToDictionary(p => p.pieceType);
    }

    public PieceData Get(PieceType type)
    {
        return dict[type];
    }
}