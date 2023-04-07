using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MyNetworkManager : NetworkManager
{
    [Header("MyNetworkManager")]
    [SerializeField] GameObject basePrefab;

    [SerializeField] GameObject teamColorSelectorPrefab;
    [SerializeField] float teamColorSelectorSpawnDelay = 0.1f;

    [Scene]
    [SerializeField] string gameScene;

    [Header("Testing")]
    [SerializeField] bool canStartWith1Player = false;
    public bool CanStartWith1Player => canStartWith1Player;

    public static event Action ClientOnConnected;
    public static event Action ClientOnDisconnected;

    bool isGameInProgress = false;

    float lobbyCreatedTime;
    float lobbyTimer;  // Static doesn't work either
    public float LobbyTimer => lobbyTimer;

    List<MyPlayer> players = new List<MyPlayer>();
    public List<MyPlayer> Players => players;

    List<MyPlayer> remainingPlayers;
    public List<MyPlayer> RemainingPlayers => remainingPlayers;

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

        players.Remove(player);

        base.OnServerDisconnect(conn);
    }

    public override void OnStopServer()
    {
        players.Clear();

        isGameInProgress = false;
    }

    public void StartGame()
    {
        if (players.Count < 2 && !canStartWith1Player) { return; }

        isGameInProgress = true;

        ServerChangeScene(gameScene);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        MyPlayer player = conn.identity.GetComponent<MyPlayer>();

        players.Add(player);

        player.SetDisplayName($"Player {players.Count}");


        if (players.Count == 1)
        {
            player.SetPartyOwner(true);
            lobbyCreatedTime = Time.time;

            StartCoroutine(SpawnTeamColorSelector());
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

            foreach (MyPlayer player in players)
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

    private IEnumerator SpawnTeamColorSelector()
    {
        yield return new WaitForSeconds(teamColorSelectorSpawnDelay);
        Canvas lobbyCanvas = FindObjectOfType<LobbyMenu>().LobbyCanvas;
        GameObject teamColorSelectorInstance = Instantiate(teamColorSelectorPrefab, lobbyCanvas.transform);
        NetworkServer.Spawn(teamColorSelectorInstance);  // Provide connectionToClient?
    }

    [Server]
    public void RemovePlayerFromRemainingPlayers(MyPlayer player)
    {
        remainingPlayers.Remove(player);
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
        players.Clear();
    }

    #endregion
}
