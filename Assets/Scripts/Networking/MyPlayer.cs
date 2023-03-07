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

    public override void OnStartClient()
    {
        if (!isClientOnly) { return; }

        Unit.AuthorityOnUnitSpawned += AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned += AuthorityHandleUnitDespawned;
    }

    public override void OnStopClient()
    {
        if (!isClientOnly) { return; }

        Unit.AuthorityOnUnitSpawned -= AuthorityHandleUnitSpawned;
        Unit.AuthorityOnUnitDespawned -= AuthorityHandleUnitDespawned;
    }

    private void AuthorityHandleUnitSpawned(Unit unit)  // Necessary?
    {
        if (!isOwned) { return; }

        myUnits.Add(unit);
        // TODO: Add here too?
    }

    private void AuthorityHandleUnitDespawned(Unit unit)  // Necessary?
    {
        if (!isOwned) { return; }

        myUnits.Remove(unit);
        // TODO: Add here too?
    }

    #endregion
}
