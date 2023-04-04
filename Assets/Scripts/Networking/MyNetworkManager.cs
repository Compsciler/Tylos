using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [SerializeField] GameObject basePrefab;

    public static event Action ClientOnConnected;
    public static event Action ClientOnDisconnected;

    bool isGameInProgress = false;

    List<MyPlayer> players = new List<MyPlayer>();
    public List<MyPlayer> Players => players;

    NetworkConnectionToClient hostConnection = null;

    public static event Action<ObjectIdentity> ServerOnPlayerIdentityUpdated;


    #region Server
    
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        if (!isGameInProgress) { return; }

        conn.Disconnect();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        MyPlayer player = conn.identity.GetComponent<MyPlayer>();

        Players.Remove(player);

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        Players.Clear();
        
        isGameInProgress = false;
    }

    public void StartGame()
    {
        if (Players.Count < 2) { return; }

        isGameInProgress = true;

        ServerChangeScene("GameScene");
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        MyPlayer player = conn.identity.GetComponent<MyPlayer>();

        Players.Add(player);

        player.SetDisplayName($"Player {Players.Count}");

        player.SetPartyOwner(Players.Count == 1);

        // TODO: move into StartGame() later
        // If this is the first player to join, make them the host
        if (hostConnection == null)
        {
            hostConnection = conn;
        }

        IdentityInfo playerIdentity = SetAndGetPlayerIdentity(conn);

        GameObject baseInstance = Instantiate(basePrefab, conn.identity.transform.position, conn.identity.transform.rotation);
        SetBaseIdentityToPlayerIdentity(baseInstance, playerIdentity);  // If you move this line to after the Spawn() call, the base will be the wrong color for a few frames somehow
        NetworkServer.Spawn(baseInstance, conn);
    }

    private IdentityInfo SetAndGetPlayerIdentity(NetworkConnectionToClient conn)
    {
        MyPlayer player = conn.identity.GetComponent<MyPlayer>();
        Color randomColor = TeamColorAssigner.Instance.GetAndRemoveRandomColor();
        player.SetTeamColor(randomColor);

        ObjectIdentity playerObjectIdentity = player.GetComponent<ObjectIdentity>();
        IdentityInfo playerIdentity = new IdentityInfo(randomColor);
        playerObjectIdentity.SetIdentity(playerIdentity);
        ServerOnPlayerIdentityUpdated?.Invoke(playerObjectIdentity);
        return playerIdentity;
    }

    private void SetBaseIdentityToPlayerIdentity(GameObject baseInstance, IdentityInfo playerIdentity)
    {
        ObjectIdentity baseObjectIdentity = baseInstance.GetComponent<ObjectIdentity>();
        baseObjectIdentity.SetIdentity(playerIdentity);
    }
    
    #endregion
    
    #region Client
    
    public override void OnClientConnect()
    {
        base.OnClientConnect();

        ClientOnConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        ClientOnDisconnected?.Invoke();
    }

    public override void OnStopClient()
    {
        Players.Clear();
    }

    #endregion
}
