using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using static GameStats;

[RequireComponent(typeof(PlayerArmies))]
public class MyPlayer : NetworkBehaviour
{
    [SerializeField] Transform cameraTransform;
    public Transform CameraTransform => cameraTransform;

    int playerId = -1;

    GameStats stats = new GameStats();
    List<Army> myArmies = new List<Army>();
    public List<Army> MyArmies => myArmies;
    List<Base> myBases = new List<Base>();
    public List<Base> MyBases => myBases;

    PlayerArmies myPlayerArmies;

    ObjectIdentity playerIdentity;
    Color teamColor = new Color();
    public Color TeamColor => teamColor;


    [SyncVar(hook = nameof(AuthorityHandlePartyOwnerStateUpdated))]
    bool isPartyOwner = false;
    public bool IsPartyOwner => isPartyOwner;

    [SyncVar(hook = nameof(ClientHandleDisplayNameUpdated))]
    string displayName;
    public string DisplayName => displayName;

    public static event Action ClientOnInfoUpdated;
    public static event Action<bool> AuthorityOnPartyOwnerStateUpdated;


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
    }

    #endregion

    #region Client

    public override void OnStartAuthority()  // Start() for objects the client owns (equivalent to OnStartClient() with !isOwned guard)
    {
        if (NetworkServer.active) { return; }  // Return if this is running as the server (before isClientOnly is set)

        Army.AuthorityOnArmySpawned += AuthorityHandleArmySpawned;
        Army.AuthorityOnArmyDespawned += AuthorityHandleArmyDespawned;
        Base.AuthorityOnBaseSpawned += AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned += AuthorityHandleBaseDespawned;
    }

    public override void OnStartClient()
    {
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

    #endregion
}
