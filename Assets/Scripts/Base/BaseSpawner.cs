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
    private GameObject SpawnArmy(IdentityInfo identity, int count)
    {
        GameObject armyInstance = Instantiate(
            armyPrefab,
            armySpawnPoint.position,
            armySpawnPoint.rotation);
        armyInstance.GetComponent<ObjectIdentity>().SetIdentity(identity);

        Army army = armyInstance.GetComponent<Army>();
        army.SetUnits(identity, count);
        NetworkServer.Spawn(armyInstance, connectionToClient);
        return armyInstance;
    }

    /// <summary>
    /// Spawns an army with <count> number of units with <identity> and moves it to <position>
    /// </summary>
    /// <param name="identity"> IdentityInfo of each unit. This mean every unit in the army will start with an identical unit</param>
    /// <param name="count"> Number of units the army will spawn with </param>
    /// <param name="position">Target movement position </param>
    [Command]
    public void CmdSpawnMoveArmy(IdentityInfo identity, int count, Vector3 position)
    {
        GameObject armyObject = SpawnArmy(identity, count);
        Army army = armyObject.GetComponent<Army>();
        RpcOnSpawnMoveArmy(army, position);
    }

    [TargetRpc]
    public void RpcOnSpawnMoveArmy(Entity entity, Vector3 position)
    {
        SelectionHandler.AddToSelection(entity);
        entity.TryMove(position); // This will call CmdMove() on the server, so I'm not sure if I should call it here, but calling it in CmdSpawnMoveArmy() doesn't work
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
