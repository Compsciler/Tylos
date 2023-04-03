using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Unit
{
    [SerializeField] IdentityInfo identityInfo;

    public IdentityInfo IdentityInfo
    {
        get => identityInfo;
        set => identityInfo = value;
    }
    
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
}   
