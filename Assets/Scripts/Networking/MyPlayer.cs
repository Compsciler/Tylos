using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerArmies))]
public class MyPlayer : NetworkBehaviour
{
    int playerId = -1;

    List<Unit> myUnits = new List<Unit>();
    public List<Unit> MyUnits => myUnits;
    List<Base> myBases = new List<Base>();
    public List<Base> MyBases => myBases;

    PlayerArmies myPlayerArmies;

    void Awake()
    {
        myPlayerArmies = GetComponent<PlayerArmies>();
    }

    #region Server

    public override void OnStartServer()
    {
        playerId = connectionToClient.connectionId;

        Unit.ServerOnUnitSpawned += ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned += ServerHandleUnitDespawned;
        Base.ServerOnBaseSpawned += ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned += ServerHandleBaseDespawned;
    }

    public override void OnStopServer()
    {
        Unit.ServerOnUnitSpawned -= ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned -= ServerHandleUnitDespawned;
        Base.ServerOnBaseSpawned -= ServerHandleBaseSpawned;
        Base.ServerOnBaseDespawned -= ServerHandleBaseDespawned;
    }

    private void ServerHandleUnitSpawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myUnits.Add(unit);
        myPlayerArmies.AddUnitToNewArmy(unit);
    }

    private void ServerHandleUnitDespawned(Unit unit)
    {
        if (unit.connectionToClient.connectionId != connectionToClient.connectionId) { return; }

        myUnits.Remove(unit);
        myPlayerArmies.RemoveUnitFromArmy(unit);
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

        Unit.AuthorityOnUnitSpawned += AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned += AuthorityHandleUnitDespawned;
        Base.AuthorityOnBaseSpawned += AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned += AuthorityHandleBaseDespawned;
    }

    public override void OnStopClient()  // OnStopAuthority() is only called when authority is removed, which can happen even if the object is not destroyed
    {
        if (!isOwned || !isClientOnly) { return; }  // Return if not owned by this client or this is the server

        Unit.AuthorityOnUnitSpawned -= AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned -= AuthorityHandleUnitDespawned;
        Base.AuthorityOnBaseSpawned -= AuthorityHandleBaseSpawned;
        Base.AuthorityOnBaseDespawned -= AuthorityHandleBaseDespawned;
    }

    private void AuthorityHandleUnitSpawned(Unit unit)  // Necessary?
    {
        myUnits.Add(unit);
        // TODO: Add here too?
    }

    private void AuthorityHandleUnitDespawned(Unit unit)  // Necessary?
    {
        myUnits.Remove(unit);
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
