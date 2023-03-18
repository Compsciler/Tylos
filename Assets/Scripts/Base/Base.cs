using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Base : NetworkBehaviour
{
    // [SerializeField] int unitCost = 25;

    public static event Action<Base> ServerOnBaseSpawned;
    public static event Action<Base> ServerOnBaseDespawned;

    public static event Action<Base> AuthorityOnBaseSpawned;
    public static event Action<Base> AuthorityOnBaseDespawned;


    #region Server

    public override void OnStartServer()
    {
        ServerOnBaseSpawned?.Invoke(this);
    }

    public override void OnStopServer()
    {
        ServerOnBaseDespawned?.Invoke(this);
    }

    #endregion

    #region Client

    public override void OnStartAuthority()
    {
        AuthorityOnBaseSpawned?.Invoke(this);
    }

    public override void OnStopClient()
    {
        if (!isOwned) { return; }

        AuthorityOnBaseDespawned?.Invoke(this);
    }

    #endregion
}
