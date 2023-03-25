using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerArmies))]
public class MyPlayer : NetworkBehaviour
{
    int playerId = -1;

    List<Army> myArmies = new List<Army>();
    public List<Army> MyArmies => myArmies;
    List<Base> myBases = new List<Base>();
    public List<Base> MyBases => myBases;

    PlayerArmies myPlayerArmies;

    ObjectIdentity playerIdentity;
    Color teamColor = new Color();
    public Color TeamColor => teamColor;

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
    }

    public override void OnStopServer()
    {
        Army.ServerOnArmySpawned -= ServerHandleArmySpawned;
        Army.ServerOnArmyDespawned -= ServerHandleArmyDespawned;
        Base.ServerOnBaseSpawned -= ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned -= ServerHandleBaseDespawned;
    }

    [Server]
    public void SetTeamColor(Color color)
    {
        teamColor = color;
    }

    private void ServerHandleArmySpawned(Army army)
    {
        if (army.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myArmies.Add(army);
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
    }

    private void ServerHandleBaseDespawned(Base base_)
    {
        if (base_.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myBases.Remove(base_);
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

    public override void OnStopClient()  // OnStopAuthority() is only called when authority is removed, which can happen even if the object is not destroyed
    {
        if (!isOwned || !isClientOnly) { return; }  // Return if not owned by this client or this is the server

        Army.AuthorityOnArmySpawned -= AuthorityHandleArmySpawned;
        Army.AuthorityOnArmyDespawned -= AuthorityHandleArmyDespawned;
        Base.AuthorityOnBaseSpawned -= AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned -= AuthorityHandleBaseDespawned;
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
