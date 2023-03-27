using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ObjectIdentity))]
public class BaseSpawner : NetworkBehaviour
{

    [SerializeField] private GameObject armyPrefab;
    [SerializeField] private Transform armySpawnPoint;

    #region Server
    [Server]
    private GameObject SpawnArmy(Unit[] units)
    {
        GameObject armyInstance = Instantiate(
            armyPrefab,
            armySpawnPoint.position,
            armySpawnPoint.rotation);
        IdentityInfo baseIdentity = GetComponent<ObjectIdentity>().Identity;  // TODO: move to OnServerStart() if base identity doesn't change and hope race condition doesn't happen
        armyInstance.GetComponent<ObjectIdentity>().SetIdentity(baseIdentity);
        Army army = armyInstance.GetComponent<Army>();
        army.SetUnits(units);
        NetworkServer.Spawn(armyInstance, connectionToClient);
        return armyInstance;
    }

    [Command]
    public void CmdSpawnMoveArmy(Unit[] units, Vector3 position)
    {
        GameObject armyObject = SpawnArmy(units);
        Army army = armyObject.GetComponent<Army>();
        SelectionHandler.AddToSelection(army);
        army.TryMove(position);
    }

    #endregion

    #region Client

    /*
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) { return; }

        if (!isOwned) { return; }

        CmdSpawnUnit();
    }
    */

    #endregion
}
