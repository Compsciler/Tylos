using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using static GameStats;

[RequireComponent(typeof(PlayerArmies))]
public class MyPlayer : NetworkBehaviour
{
    [SerializeField] Transform cameraTransform;
    public Transform CameraTransform => cameraTransform;

    // Is currently unused and set to the connectionId, but could later be used for team modes (would have to refactor connectionToClient.connectionId)
    int playerId = -1;

    GameStats stats = new GameStats();
    [SerializeField] int BaseCreationCost = 5;
    List<Army> myArmies = new List<Army>();
    public List<Army> MyArmies => myArmies;
    List<Base> myBases = new List<Base>();
    public List<Base> MyBases => myBases;

    [SerializeField] public Material fog;

    [SerializeField] public int fogResolutionX = 32;
    [SerializeField] public int fogResolutionY = 32;
    [SerializeField] public float viewDistance = 4;
    Texture2D fogTex;

    PlayerArmies myPlayerArmies;

    ObjectIdentity playerIdentity;
    Color teamColor = new Color();
    public Color TeamColor => teamColor;

    private Controls controls;


    [SyncVar(hook = nameof(AuthorityHandlePartyOwnerStateUpdated))]
    bool isPartyOwner = false;
    public bool IsPartyOwner => isPartyOwner;

    [SyncVar(hook = nameof(ClientHandleDisplayNameUpdated))]
    string displayName;
    public string DisplayName => displayName;

    public static event Action ClientOnInfoUpdated;
    public static event Action<bool> AuthorityOnPartyOwnerStateUpdated;

    public static event Action<Base> ServerOnPlayerHandledBaseDespawned;


    void Awake()
    {
        myPlayerArmies = GetComponent<PlayerArmies>();
    }

    #region Server

    public override void OnStartServer()
    {
        playerId = connectionToClient.connectionId;

        Army.ServerOnArmySpawned += ServerHandleArmySpawned;
        Army.ServerOnArmyDespawned += ServerHandleArmyDespawned;
        Base.ServerOnBaseSpawned += ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned += ServerHandleBaseDespawned;

        DontDestroyOnLoad(gameObject);
    }

    public override void OnStopServer()
    {
        Army.ServerOnArmySpawned -= ServerHandleArmySpawned;
        Army.ServerOnArmyDespawned -= ServerHandleArmyDespawned;
        Base.ServerOnBaseSpawned -= ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned -= ServerHandleBaseDespawned;
    }

    [Server]
    public void SetDisplayName(string displayName)
    {
        this.displayName = displayName;
    }

    [Server]
    public void SetPartyOwner(bool state)
    {
        isPartyOwner = state;
    }

    [Server]
    public void SetTeamColor(Color color)
    {
        teamColor = color;
    }

    [Command]
    public void CmdStartGame()
    {
        if (!isPartyOwner) { return; }

        ((MyNetworkManager)NetworkManager.singleton).StartGame();
    }

    private void ServerHandleArmySpawned(Army army)
    {
        if (army.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myArmies.Add(army);
        stats.AddUnitsCreated(connectionToClient.connectionId, army.ArmyUnits.Count);
        // myPlayerArmies.AddUnitToNewArmy(unit);
        // Adding and removing units from armies is done in the Army class
    }

    private void ServerHandleArmyDespawned(Army army)
    {
        if (army.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myArmies.Remove(army);
        // myPlayerArmies.RemoveUnitFromArmy(unit);
        // Adding and removing units from armies is done in the Army class
    }

    private void ServerHandleBaseSpawned(Base base_)
    {
        if (base_.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBases.Add(base_);
        stats.AddBaseCreated(connectionToClient.connectionId);
    }

    private void ServerHandleBaseDespawned(Base base_)
    {
        if (base_.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBases.Remove(base_);
        stats.AddBaseDestroyed(connectionToClient.connectionId);
        ServerOnPlayerHandledBaseDespawned?.Invoke(base_);
    }

    #endregion

    #region Client

    public override void OnStartAuthority()  // Start() for objects the client owns (equivalent to OnStartClient() with !isOwned guard)
    {
        setupFog();
        if (NetworkServer.active) { return; }  // Return if this is running as the server (before isClientOnly is set)

        Army.AuthorityOnArmySpawned += AuthorityHandleArmySpawned;
        Army.AuthorityOnArmyDespawned += AuthorityHandleArmyDespawned;
        Base.AuthorityOnBaseSpawned += AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned += AuthorityHandleBaseDespawned;
    }
    private void setupFog()
    {
        fogTex = new Texture2D(fogResolutionX, fogResolutionY);
        fogTex.wrapModeU = TextureWrapMode.Clamp;
        fogTex.wrapModeV = TextureWrapMode.Clamp;

        fog.SetTexture("FogTex", fogTex);
    }

    [ClientCallback]
    void Update()
    {
        float[] fogVals = new float[fogResolutionX * fogResolutionY];
        for (int i = 0; i < fogResolutionX * fogResolutionY; i++)
        {
            fogVals[i] = 1;
        }

        foreach (Army army in myArmies)
        {
            for (int x = 0; x < fogResolutionX; x++)
            {
                for (int y = 0; y < fogResolutionY; y++)
                {
                    float game_x = -(((float)x / fogResolutionX) * 20 - 10);
                    float game_y = -(((float)y / fogResolutionY) * 20 - 10);
                    double d_x = army.transform.position.x - game_x;
                    double d_y = army.transform.position.z - game_y;
                    double dist = Math.Sqrt(d_x * d_x + d_y * d_y) / viewDistance / 2;
                    fogVals[x + y * fogResolutionX] = Math.Min(fogVals[x + y * fogResolutionX], (float)dist);
                }
            }
        }

        foreach (Base b in myBases)
        {
            for (int x = 0; x < fogResolutionX; x++)
            {
                for (int y = 0; y < fogResolutionY; y++)
                {
                    float game_x = -(((float)x / fogResolutionX) * 20 - 10);
                    float game_y = -(((float)y / fogResolutionY) * 20 - 10);
                    double d_x = b.transform.position.x - game_x;
                    double d_y = b.transform.position.z - game_y;
                    double dist = Math.Sqrt(d_x * d_x + d_y * d_y) / viewDistance / 2;
                    fogVals[x + y * fogResolutionX] = Math.Min(fogVals[x + y * fogResolutionX], (float)dist);
                }
            }
        }

        for (int x = 0; x < fogResolutionX; x++)
        {
            for (int y = 0; y < fogResolutionY; y++)
            {
                fogTex.SetPixel(x, y, new Color(fogVals[x + y * fogResolutionX], 0, 0, 0));
            }
        }
        fogTex.Apply(true, false);
    }
    public override void OnStartClient()
    {
        controls = new Controls();
        controls.Player.MakeBase.performed += makeBase;
        controls.Enable();

        if (NetworkServer.active) { return; }

        DontDestroyOnLoad(gameObject);

        ((MyNetworkManager)NetworkManager.singleton).Players.Add(this);
    }

    public override void OnStopClient()  // OnStopAuthority() is only called when authority is removed, which can happen even if the object is not destroyed
    {
        ClientOnInfoUpdated?.Invoke();

        if (!isClientOnly) { return; }  //  Return this is not the server

        ((MyNetworkManager)NetworkManager.singleton).Players.Remove(this);

        if (!isOwned) { return; }  // Return if not owned by this client

        Army.AuthorityOnArmySpawned -= AuthorityHandleArmySpawned;
        Army.AuthorityOnArmyDespawned -= AuthorityHandleArmyDespawned;
        Base.AuthorityOnBaseSpawned -= AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned -= AuthorityHandleBaseDespawned;
    }

    private void ClientHandleDisplayNameUpdated(string oldDisplayName, string newDisplayName)
    {
        ClientOnInfoUpdated?.Invoke();
    }

    private void AuthorityHandlePartyOwnerStateUpdated(bool oldState, bool newState)
    {
        if (!isOwned) { return; }

        AuthorityOnPartyOwnerStateUpdated?.Invoke(newState);
    }

    private void AuthorityHandleArmySpawned(Army army)  // Necessary?
    {
        myArmies.Add(army);
        // TODO: Add here too?
    }

    private void AuthorityHandleArmyDespawned(Army army)  // Necessary?
    {
        myArmies.Remove(army);
        // TODO: Add here too?
    }

    private void AuthorityHandleBaseSpawned(Base base_)
    {
        myBases.Add(base_);
    }

    private void AuthorityHandleBaseDespawned(Base base_)
    {
        myBases.Remove(base_);
    }
    [SerializeField] private GameObject basePrefab;
    private void makeBase(InputAction.CallbackContext input)
    {
        foreach (Entity e in SelectionHandler.SelectedEntities)
        {
            if (e is Army army)
            {
                if (army.ArmyUnits.Count > BaseCreationCost)
                {
                    for (int i = 0; i < BaseCreationCost; i++)
                    {
                        army.ArmyUnits.RemoveAt(0);
                    }
                ((MyNetworkManager)NetworkManager.singleton).MakeBase(this, army.transform.position);
                }
            }

        }
    }

    #endregion
}
