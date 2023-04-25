using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Resisted the urge to name this EntityIdentity
public class ObjectIdentity : NetworkBehaviour
{
    [SyncVar(hook = nameof(HandleIdentityUpdated))]
    [SerializeField]
    IdentityInfo identity;
    public IdentityInfo Identity => identity;

    [SyncVar(hook = nameof(HandleTeamIdentityUpdated))]
    [SerializeField]
    IdentityInfo teamIdentity; 
    public IdentityInfo TeamIdentity => teamIdentity;

    public static event Action<ObjectIdentity> ServerOnTeamIdentityUpdated;

    public event Action<ObjectIdentity> ServerOnIdentityUpdated;


    public Color GetColorFromIdentity()
    {
        return new Color(identity.r, identity.g, identity.b);
    }

    #region Server

    [Server]
    public void SetIdentity(IdentityInfo identity)
    {
        this.identity = identity;
    }

    [Server]
    public void SetTeamIdentity(IdentityInfo identity)
    {
        teamIdentity = identity;
    }

    [Server]
    public void SetIdentity(float r, float g, float b)
    {
        SetIdentity(new IdentityInfo(r, g, b));
    }

    #endregion

    #region Client

    private void HandleTeamIdentityUpdated(IdentityInfo oldIdentity, IdentityInfo newIdentity)
    {
        ServerOnTeamIdentityUpdated?.Invoke(this);
    }

    [Client]
    private void HandleIdentityUpdated(IdentityInfo oldIdentity, IdentityInfo newIdentity)
    {
        ServerOnIdentityUpdated?.Invoke(this);
    }

    #endregion
}

[Serializable]
public struct IdentityInfo
{
    public float r;
    public float g;
    public float b;

    public IdentityInfo(float r, float g, float b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
    public IdentityInfo(Color color)
    {
        r = color.r;
        g = color.g;
        b = color.b;
    }
    public IdentityInfo(IdentityInfo identity)
    {
        r = identity.r;
        g = identity.g;
        b = identity.b;
    }

    public Color GetColor()
    {
        return new Color(r, g, b);
    }
}
