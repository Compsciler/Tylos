using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(ObjectIdentity))]
public class IdentityColorRendererSetter : ColorRendererSetter
{
    ObjectIdentity objectIdentity;

    #region Server
    public override void OnStartServer()
    {
        base.OnStartServer();
        GetComponent<ObjectIdentity>().ServerOnIdentityUpdated += ServerHandleIdentityUpdated;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        GetComponent<ObjectIdentity>().ServerOnIdentityUpdated -= ServerHandleIdentityUpdated;
    }

    [Server]
    protected override Color GetColorToSet()
    {
        ObjectIdentity identity = GetComponent<ObjectIdentity>();

        return identity.GetColorFromIdentity();
    }

    [Server]
    private void ServerHandleIdentityUpdated(ObjectIdentity identity)
    {
        color = identity.GetColorFromIdentity();
    }
    #endregion
}
