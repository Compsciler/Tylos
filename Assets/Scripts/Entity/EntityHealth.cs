using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public class EntityHealth : NetworkBehaviour
{
    [SyncVar]
    private float health = 30f;
    public float Health => health;

    public UnityEvent OnDie;
    public UnityEvent OnTakeDamage;

    #region Server

    [Server]
    public virtual void TakeDamage(float damage)
    {
        OnTakeDamage.Invoke();
        Debug.Log(gameObject.name + " took " + damage + " damage");
        health -= damage;
        Debug.Log("Health: " + health);
        if (health <= 0)
        {
            Die();
        }
    }

    [Server]
    protected virtual void Die()
    {
        Debug.Log(gameObject.name + " died");
        OnDie.Invoke();
        NetworkServer.Destroy(gameObject);  
        Debug.Log("Destroyed " + gameObject.name);
    }

    #endregion
}
