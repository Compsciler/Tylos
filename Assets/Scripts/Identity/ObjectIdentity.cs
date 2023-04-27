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
    public Vector3 IdentityVec3 => new Vector3(identity.r, identity.g, identity.b);

    [SyncVar(hook = nameof(HandleTeamIdentityUpdated))]
    [SerializeField]
    IdentityInfo teamIdentity;
    public IdentityInfo TeamIdentity => teamIdentity;

    public static event Action<ObjectIdentity> ServerOnTeamIdentityUpdated;

    public event Action<ObjectIdentity> ServerOnIdentityUpdated;

    public event Action<ObjectIdentity> ClientOnIdentityUpdated;


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
    public void SetIdentity(float r, float g, float b)
    {
        SetIdentity(new IdentityInfo(r, g, b));
    }

    [Server]
    public void SetIdentity(Vector3 color)
    {
        SetIdentity(new IdentityInfo(color.x, color.y, color.z));
    }

    [Server]
    public void SetTeamIdentity(IdentityInfo identity)
    {
        teamIdentity = identity;
    }

    public Vector3 GetIdentityVector3()
    {
        return new Vector3(identity.r, identity.g, identity.b);
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
        ClientOnIdentityUpdated?.Invoke(this);
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
