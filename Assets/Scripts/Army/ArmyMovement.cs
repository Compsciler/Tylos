using Mirror;
using UnityEngine;
using UnityEngine.AI;
public class ArmyMovement : EntityMovement
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
