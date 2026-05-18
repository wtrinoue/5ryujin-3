using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class NetworkRecordManager : NetworkBehaviour
{
    [SerializeField] private RecordManager recordManager;
    public void SetRecordManager(RecordManager rm)
    {
        recordManager = rm;
    }
    public void SendAddMove(
        PieceType pieceType,
        int rotation,
        bool flipped,
        int x,
        int y,
        bool player,
        bool touchdown
    )
    {
        RpcAddMove((int)pieceType, rotation, flipped, x, y, player, touchdown);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcAddMove(
        int pieceType,
        int rotation,
        bool flipped,
        int x,
        int y,
        bool player,
        bool touchdown
    )
    {
        recordManager.AddMove(
            (PieceType)pieceType,
            rotation,
            flipped,
            x,
            y,
            player,
            touchdown
        );

        recordManager.SaveRecord();
    }
}
