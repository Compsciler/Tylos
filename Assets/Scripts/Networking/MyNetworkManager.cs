using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class MyNetworkManager : NetworkManager
{
    [Header("MyNetworkManager")]
    [SerializeField] GameObject basePrefab;
    [SerializeField] GameObject armyPrefab;
    [SerializeField] GameOverHandler gameOverHandler;
    [SerializeField] private int numWildArmyGenerationAttempts = 60;
    private const float HexExtentZ = 36f;
    private const float HexExtentX = 40f;

    [Scene]
    [SerializeField] string gameScene;

    [Header("Steam")]
    [SerializeField] bool useSteam = false;
    public bool UseSteam => useSteam;

    public static ulong LobbyId { get; set; }

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

    public int MaxConnections => maxConnections;


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
        remainingPlayers = new List<MyPlayer>(players);

        ServerChangeScene(gameScene);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        MyPlayer player = conn.identity.GetComponent<MyPlayer>();

        players.Add(player);

        if (useSteam)
        {
            CSteamID steamId = SteamMatchmaking.GetLobbyMemberByIndex(
                new CSteamID(LobbyId),
                numPlayers - 1
            );
            player.SetDisplayName($"{SteamFriends.GetFriendPersonaName(steamId)}");
        }
        else
        {
            player.SetDisplayName($"Player {players.Count}");
        }

        if (players.Count == 1)
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
            GameOverHandler gameOverHandlerInstance = Instantiate(gameOverHandler);

            NetworkServer.Spawn(gameOverHandlerInstance.gameObject);

            foreach (MyPlayer player in players)
            {
                SetAndGetPlayerIdentity(player);

                MakeBase(player, GetStartPosition().position);
            }

            for (int i = 0; i < numWildArmyGenerationAttempts; i++)
            {
                var pos = new Vector3(Random.Range(-HexExtentX, HexExtentX), 1, Random.Range(-HexExtentZ, HexExtentZ));
                if (Physics.Raycast(pos, Vector3.down, 10f))
                {
                    pos.y = 0;
                    SpawnArmy(new IdentityInfo(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f)),
                        Random.Range(1, 3), pos);
                }
            }
        }
    }

    // duplicated code from spawners
    // refactor later but this shouldn't be too bad
    [Server]
    public GameObject SpawnArmy(IdentityInfo identity, int count, Vector3 spawnPos)
    {
        GameObject armyInstance = Instantiate(
            armyPrefab,
            spawnPos,
            Quaternion.identity);
        ObjectIdentity objectIdentity = armyInstance.GetComponent<ObjectIdentity>();
        objectIdentity.SetIdentity(identity);
        objectIdentity.SetTeamIdentity(identity);

        Army army = armyInstance.GetComponent<Army>();
        army.SetUnits(identity, count);
        NetworkServer.Spawn(armyInstance);
        return armyInstance;
    }

    [Server]
    public void MakeBase(MyPlayer player, Army army, int BaseCreationCost)
    {
        IdentityInfo armyIdentity = new IdentityInfo();
        armyIdentity = army.GetComponent<ObjectIdentity>().Identity;
        GameObject baseInstance = Instantiate(
                    basePrefab,
                    army.transform.position,
                    Quaternion.identity);

        List<Unit> unitsToKill = new List<Unit>();
        for (int i = 0; i < BaseCreationCost; i++)
        {
            unitsToKill.Add(army.ArmyUnits[i]);
        }
        army.KillUnits(unitsToKill);
        IdentityInfo playerIdentity = player.GetComponent<ObjectIdentity>().Identity;
        SetBaseIdentityToPlayerIdentity(baseInstance, armyIdentity, playerIdentity);  // If you move this line to after the Spawn() call, the base will be the wrong color for a few frames somehow

        NetworkServer.Spawn(baseInstance, player.connectionToClient);
    }
    

    [Server]
    public void MakeBase(MyPlayer player, Vector3 position)
    {
        GameObject baseInstance = Instantiate(
            basePrefab,
            position,
            Quaternion.identity);

        IdentityInfo playerIdentity = player.GetComponent<ObjectIdentity>().Identity;
        SetBaseIdentityToPlayerIdentity(baseInstance, playerIdentity, playerIdentity);  // If you move this line to after the Spawn() call, the base will be the wrong color for a few frames somehow

        NetworkServer.Spawn(baseInstance, player.connectionToClient);
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

    private void SetBaseIdentityToPlayerIdentity(GameObject baseInstance, IdentityInfo baseIdentity, IdentityInfo teamIdentity)
    {
        ObjectIdentity baseObjectIdentity = baseInstance.GetComponent<ObjectIdentity>();
        baseObjectIdentity.SetIdentity(baseIdentity);
        baseObjectIdentity.SetTeamIdentity(teamIdentity);
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
