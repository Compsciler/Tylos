using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Unit
{
    [SyncVar] IdentityInfo identityInfo;
    [SyncVar] public float health = 1f;
    public float Health => health;
    
    public IdentityInfo IdentityInfo => identityInfo;
    // TODO: Add unit specific data her

    public Unit()
    {
        identityInfo = new IdentityInfo();
    }

    public Unit(IdentityInfo identityInfo)
    {
        this.identityInfo = identityInfo;
    }

    public void SetIdentityInfo(IdentityInfo identityInfo)
    {
        this.identityInfo = identityInfo;
    }

    public void SetHealth(float health)
    {
        Debug.Log("Setting health to " + health);
        this.health = health;
    }
}   
