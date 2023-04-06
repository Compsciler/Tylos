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
        ArmyMovement armyMovement = armyObject.GetComponent<ArmyMovement>();
        armyMovement.Move(position);
        RpcAddToSelection(armyObject.GetComponent<Entity>());
    }

    #endregion
    #region Client

    [TargetRpc]
    public void RpcAddToSelection(Entity entity)
    {
        SelectionHandler.AddToSelection(entity);
    }
    #endregion
}
