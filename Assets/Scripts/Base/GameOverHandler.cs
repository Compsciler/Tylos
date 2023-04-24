using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GameOverHandler : NetworkBehaviour
{
    public static event Action ServerOnGameOver;
    public static event Action<string> ClientOnGameOver;

    public static event Action<string> ClientOnPlayerLost;
    public static event Action TargetClientOnPlayerLost;
    public static event Action TargetClientOnPlayerWon;

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
        int basePlayerId = basePlayer.connectionToClient.connectionId;
        List<Base> playerBases = basePlayer.MyBases;
        Debug.Log($"Player {basePlayerId} has {playerBases.Count} bases");

        if (playerBases.Count > 0) { return; }

        networkManager.RemovePlayerFromRemainingPlayers(basePlayer);
        // TODO: Invoke event to notify players of eliminated player

        TargetRpcPlayerLost(basePlayer.connectionToClient);

        Debug.Log($"Remaining players: {networkManager.RemainingPlayers.Count}");

        if (networkManager.RemainingPlayers.Count > 1) { return; }

        MyPlayer winningPlayer = networkManager.RemainingPlayers[0];
        int winningPlayerId = winningPlayer.connectionToClient.connectionId;

        string winningPlayerName = $"Player {winningPlayerId}";  // TODO: Change to player name or just id
        RpcGameOver(winningPlayerName);
        TargetRpcPlayerWon(winningPlayer.connectionToClient);

        Debug.Log($"{winningPlayer} wins");

        ServerOnGameOver?.Invoke();
    }

    #endregion

    #region Client

    [ClientRpc]
    private void RpcPlayerLost(string loser)
    {
        ClientOnPlayerLost?.Invoke(loser);
    }

    [TargetRpc]
    private void TargetRpcPlayerLost(NetworkConnectionToClient target)
    {
        TargetClientOnPlayerLost?.Invoke();
    }

    [TargetRpc]
    private void TargetRpcPlayerWon(NetworkConnectionToClient target)
    {
        TargetClientOnPlayerWon?.Invoke();
    }

    [ClientRpc]
    private void RpcGameOver(string winner)
    {
        ClientOnGameOver?.Invoke(winner);
    }

    #endregion
}
