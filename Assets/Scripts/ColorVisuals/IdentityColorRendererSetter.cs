using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(ObjectIdentity))]
public class IdentityColorRendererSetter : ColorRendererSetter
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        ObjectIdentity.ServerOnIdentityUpdated += HandleIdentityUpdated;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ObjectIdentity.ServerOnIdentityUpdated -= HandleIdentityUpdated;
    }

    private void HandleIdentityUpdated(ObjectIdentity identity)
    {
        if (identity.connectionToClient != connectionToClient) { return; }

        color = identity.GetColorFromIdentity();
    }

    [Server]
    public override Color GetColorToSet()
    {
        ObjectIdentity identity = GetComponent<ObjectIdentity>();

        return identity.GetColorFromIdentity();
    }
}
