using Mirror;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class UnitMovement : NetworkBehaviour
{
    NavMeshAgent agent;
    
    #region Server

    [Command]
    public void CmdMove(Vector3 position)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas)) { return; }

        GetComponent<NavMeshAgent>().SetDestination(hit.position);
    }

    #endregion
}
