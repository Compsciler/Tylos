using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ObjectIdentity))]
public class BaseSpawner : NetworkBehaviour
{
    [SerializeField] float spawnRate = 4f;

    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private Transform unitSpawnPoint;

    #region Server

    public override void OnStartServer()
    {
        StartCoroutine(SpawnUnits());
    }

    [Server]
    private void SpawnUnit()
    {
        GameObject unitInstance = Instantiate(
            unitPrefab,
            unitSpawnPoint.position,
            unitSpawnPoint.rotation);
        IdentityInfo baseIdentity = GetComponent<ObjectIdentity>().Identity;  // TODO: move to OnServerStart() if base identity doesn't change and hope race condition doesn't happen
        unitInstance.GetComponent<ObjectIdentity>().SetIdentity(baseIdentity);
        NetworkServer.Spawn(unitInstance, connectionToClient);
    }

    [Command]
    private void CmdSpawnUnit()
    {
        SpawnUnit();
    }

    [Server]
    private IEnumerator SpawnUnits()
    {
        while (true)
        {
            SpawnUnit();
            
            yield return new WaitForSeconds(spawnRate);
        }
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
