using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(MyPlayerArmies))]
public class MyPlayer : NetworkBehaviour
{
    List<Unit> myUnits = new List<Unit>();
    public List<Unit> MyUnits => myUnits;

    MyPlayerArmies myPlayerArmies;

    void Awake()
    {
        myPlayerArmies = GetComponent<MyPlayerArmies>();
    }

    #region Server

    public override void OnStartServer()
    {
        Unit.ServerOnUnitSpawned += ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned += ServerHandleUnitDespawned;
    }

    public override void OnStopServer()
    {
        Unit.ServerOnUnitSpawned -= ServerHandleUnitSpawned;
        Unit.ServerOnUnitDespawned -= ServerHandleUnitDespawned;
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

    #endregion

    #region Client

    public override void OnStartAuthority()  // Start() for objects the client owns (equivalent to OnStartClient() with !isOwned guard)
    {
        if (NetworkServer.active) { return; }  // Return if this is running as the server (before isClientOnly is set)

        Unit.AuthorityOnUnitSpawned += AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned += AuthorityHandleUnitDespawned;
    }

    public override void OnStopClient()  // OnStopAuthority() is only called when authority is removed, which can happen even if the object is not destroyed
    {
        if (!isOwned || !isClientOnly) { return; }  // Return if not owned by this client or this is the server

        Unit.AuthorityOnUnitSpawned -= AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned -= AuthorityHandleUnitDespawned;
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

    #endregion
}
