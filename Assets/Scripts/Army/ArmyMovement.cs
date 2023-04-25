using System;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
public class ArmyMovement : EntityMovement
{
    #region Server

    public override void OnStartServer()
    {
        GameOverHandler.TargetClientOnPlayerLost += TargetClientHandlePlayerLost;
        GameOverHandler.ServerOnGameOver += ServerHandleGameOver;
    }

    public override void OnStopServer()
    {
        GameOverHandler.TargetClientOnPlayerLost -= TargetClientHandlePlayerLost;
        GameOverHandler.ServerOnGameOver -= ServerHandleGameOver;
    }

    [Server]
    public override void Move(Vector3 position) // This function can be directly called from the server without going through the client
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 15f, NavMesh.AllAreas)) { return; } // maxDistance should be greater than the max radius of any entity in the game
        agent.SetDestination(hit.position);
    }

    [Command]
    public override void CmdMove(Vector3 position) // Client calls this function to move the their own army
    {
        Move(position);
    }


    [Command]
    private void CmdStop2()  // Not overriding CmdStop for fear of breaking something
    {
        Stop();
    }

    [Server]
    public override void Stop()
    {
        agent.ResetPath();
    }


    [Server]
    private void ServerHandleGameOver()
    {
        Stop();
    }

    #endregion

    #region Client

    [Server]
    private void TargetClientHandlePlayerLost()
    {
        CmdStop2();
    }

    #endregion
}
