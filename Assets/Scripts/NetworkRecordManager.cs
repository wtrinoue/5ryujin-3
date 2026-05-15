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
        int pieceType,
        int rotation,
        bool flipped,
        float x,
        float y,
        bool player,
        bool touchdown
    )
    {
        RpcAddMove(pieceType, rotation, flipped, x, y, player, touchdown);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcAddMove(
        int pieceType,
        int rotation,
        bool flipped,
        float x,
        float y,
        bool player,
        bool touchdown
    )
    {
        recordManager.AddMove(
            pieceType,
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
