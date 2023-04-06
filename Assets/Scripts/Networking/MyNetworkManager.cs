using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    [SerializeField] GameObject basePrefab;

    [Scene]
    [SerializeField] string gameScene;

    public static event Action ClientOnConnected;
    public static event Action ClientOnDisconnected;

    bool isGameInProgress = false;

    float lobbyCreatedTime;
    float lobbyTimer;  // Static doesn't work either
    public float LobbyTimer => lobbyTimer;

    List<MyPlayer> players = new List<MyPlayer>();
    public List<MyPlayer> Players => players;

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

        ServerChangeScene(gameScene);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        MyPlayer player = conn.identity.GetComponent<MyPlayer>();

        Players.Add(player);

        player.SetDisplayName($"Player {Players.Count}");


        if (Players.Count == 1)
        {
            player.SetPartyOwner(true);
            lobbyCreatedTime = Time.time;
        }
        else
        {
            player.SetPartyOwner(false);
        }
    }

    public override void Update()
    {
        base.Update();

        lobbyTimer = Time.time - lobbyCreatedTime;
    }

    public override void OnServerSceneChanged(string newSceneName)  // NOT OnServerChangeScene
    {
        if (newSceneName == gameScene)
        {
            // GameOverHandler gameOverHandlerInstance = Instantiate(gameOverHandler);  // TODO: Add GameOverHandler

            // NetworkServer.Spawn(gameOverHandlerInstance.gameObject);

            foreach (MyPlayer player in Players)
            {
                IdentityInfo playerIdentity = SetAndGetPlayerIdentity(player);

                GameObject baseInstance = Instantiate(
                    basePrefab,
                    GetStartPosition().position,
                    Quaternion.identity);
                SetBaseIdentityToPlayerIdentity(baseInstance, playerIdentity);  // If you move this line to after the Spawn() call, the base will be the wrong color for a few frames somehow

                NetworkServer.Spawn(baseInstance, player.connectionToClient);
            }
        }
    }

    private IdentityInfo SetAndGetPlayerIdentity(MyPlayer player)
    {
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
