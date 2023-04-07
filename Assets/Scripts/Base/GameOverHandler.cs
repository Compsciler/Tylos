using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameOverHandler : NetworkBehaviour
{
    public static event Action ServerOnGameOver;
    public static event Action<string> ClientOnGameOver;

    MyNetworkManager networkManager;

    #region Server

    public override void OnStartServer()
    {
        // Avoids race condition between player removing base from list and game over handler checking list count
        MyPlayer.ServerOnPlayerHandledBaseDespawned += ServerHandleBaseDespawned;

        networkManager = (MyNetworkManager)NetworkManager.singleton;
    }

    public override void OnStopServer()
    {
        MyPlayer.ServerOnPlayerHandledBaseDespawned -= ServerHandleBaseDespawned;
    }

    [Server]
    private void ServerHandleBaseDespawned(Base base_)
    {
        MyPlayer basePlayer = base_.connectionToClient.identity.GetComponent<MyPlayer>();
        List<Base> playerBases = basePlayer.MyBases;

        if (playerBases.Count > 0) { return; }

        networkManager.RemovePlayerFromRemainingPlayers(basePlayer);
        // TODO: Invoke event to notify players of eliminated player

        if (networkManager.RemainingPlayers.Count > 1) { return; }

        int winningPlayerId = networkManager.RemainingPlayers[0].connectionToClient.connectionId;

        RpcGameOver($"Player {winningPlayerId}");  // TODO: Change to player name or just id

        ServerOnGameOver?.Invoke();
    }

    #endregion

    #region Client

    [ClientRpc]
    private void RpcGameOver(string winner)
    {
        ClientOnGameOver?.Invoke(winner);
    }

    #endregion
}
