using System.Collections;
using System.Collections.Generic;
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
    private void SpawnArmy()
    {
        GameObject armyInstance = Instantiate(
            armyPrefab,
            armySpawnPoint.position,
            armySpawnPoint.rotation);
        IdentityInfo baseIdentity = GetComponent<ObjectIdentity>().Identity;  // TODO: move to OnServerStart() if base identity doesn't change and hope race condition doesn't happen
        armyInstance.GetComponent<ObjectIdentity>().SetIdentity(baseIdentity);
        NetworkServer.Spawn(armyInstance, connectionToClient);
    }

    [Command]
    private void CmdSpawnArmy()
    {
        SpawnArmy();
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
