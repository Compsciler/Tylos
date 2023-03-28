using UnityEngine;
using UnityEngine.AI;
using Mirror;

/// <summary>
/// Required class for all entities that can receive movement commands
/// </summary>
public class BaseMovement : EntityMovement
{
    #region Server

    [Command]
    public override void CmdMove(Vector3 position)
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 1f, NavMesh.AllAreas)) { return; } 


        agent.SetDestination(hit.position);
    }

    #endregion
}
