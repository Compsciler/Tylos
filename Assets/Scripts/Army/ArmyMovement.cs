using Mirror;
using UnityEngine;
using UnityEngine.AI;
public class ArmyMovement : EntityMovement
{
    #region Server
    [Server]
    public override void Move(Vector3 position) // This function can be directly called from the server without going through the client
    {
        if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 15f, NavMesh.AllAreas)) { return; }
        agent.SetDestination(hit.position);
    }

    [Command]
    public override void CmdMove(Vector3 position) // Client calls this function to move the their own army
    {
        Move(position);
    }

    [Server]
    public override void Stop()
    {
        agent.ResetPath();
    }

    #endregion
}
