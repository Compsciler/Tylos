using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BaseHealth : EntityHealth
{
    [SerializeField] 
    [Range(0f, 200f)]
    private float startMaxHealth = 100f;

    [SerializeField]
    [Range(0f, 200f)]
    private float maxHealth = 100f;

    public override void OnStartServer()
    {
        base.OnStartServer();
        SetHealth(startMaxHealth);
    }

    
}
