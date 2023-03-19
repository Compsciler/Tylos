using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Base))]
public class BaseSpawner : NetworkBehaviour
{
    [SerializeField] float spawnRate = 4f;

    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private Transform unitSpawnPoint;

    #region Server

    [Command]
    private void CmdSpawnUnit()
    {
        GameObject unitInstance = Instantiate(
            unitPrefab,
            unitSpawnPoint.position,
            unitSpawnPoint.rotation);
        ObjectIdentity baseIdentity = GetComponent<ObjectIdentity>();  // TODO: move to start and hope race condition doesn't happen
        unitInstance.GetComponent<ObjectIdentity>().SetIdentity(baseIdentity.GetColorFromIdentity());
        NetworkServer.Spawn(unitInstance, connectionToClient);
    }

    #endregion

    #region Client

    void Awake()
    {
        // Debug.Log("Awake: " + isServer + " " + isClient);
        
        StartCoroutine(SpawnUnits());
    }

    private IEnumerator SpawnUnits()
    {
        while (true)
        {
            CmdSpawnUnit();
            
            yield return new WaitForSeconds(spawnRate);
        }
    }

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
