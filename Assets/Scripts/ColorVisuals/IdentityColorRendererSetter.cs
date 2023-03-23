using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(ObjectIdentity))]
public class IdentityColorRendererSetter : ColorRendererSetter
{
    #region Server

    public override void OnStartServer()
    {
        base.OnStartServer();
        ObjectIdentity.ServerOnIdentityUpdated += ServerHandleIdentityUpdated;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        ObjectIdentity.ServerOnIdentityUpdated -= ServerHandleIdentityUpdated;
    }

    [Server]
    private void ServerHandleIdentityUpdated(ObjectIdentity identity)
    {
        if (identity.connectionToClient != connectionToClient) { return; }

        color = identity.GetColorFromIdentity();
    }

    [Server]
    protected override Color GetColorToSet()
    {
        ObjectIdentity identity = GetComponent<ObjectIdentity>();

        return identity.GetColorFromIdentity();
    }

    #endregion
}
