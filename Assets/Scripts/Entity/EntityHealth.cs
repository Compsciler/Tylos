using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class EntityHealth : NetworkBehaviour
{
    [SyncVar]
    private float health = 100f;
    public float Health => health;

    #region Server

    [Server]
    public void TakeDamage(float damage)
    {
        Debug.Log(gameObject.name + " took " + damage + " damage");
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    [Server]
    private void Die() {
        Debug.Log(gameObject.name + " died");
        NetworkServer.Destroy(gameObject);
    }

    #endregion
}
