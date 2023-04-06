
using Mirror;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for all entity movement scripts
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public abstract class EntityMovement : NetworkBehaviour
{
    protected NavMeshAgent agent;
    // Start is called before the first frame update
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    #region Server

    [Server]
    public virtual void Move(Vector3 position) { } // virtual instead of abstract because Mirror does not support abstract commands

    [Command]
    public virtual void CmdMove(Vector3 position) { } // virtual instead of abstract because Mirror does not support abstract commands

    [Server]
    public virtual void Stop() { } // virtual instead of abstract because Mirror does not support abstract commands

    [Command]
    public virtual void CmdStop() { } // virtual instead of abstract because Mirror does not support abstract commands
    #endregion
}