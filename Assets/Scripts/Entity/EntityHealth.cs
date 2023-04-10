using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public class EntityHealth : NetworkBehaviour
{
    [SerializeField, ] 
    [SyncVar]
    private float health = 100f;
    public float Health => health;

    [SerializeField]
    [SyncVar]
    private float maxHealth = 100f;

    public float MaxHealth => maxHealth;

    public UnityEvent OnDie;
    public UnityEvent OnTakeDamage;

    #region Server
    public override void OnStartServer()
    {
        SetHealth(maxHealth);
    }

    [Server]
    public virtual void TakeDamage(float damage)
    {
        OnTakeDamage.Invoke();
        // Debug.Log(gameObject.name + " took " + damage + " damage");
        if (Mathf.Approximately(health, 0f)) { return; }

        health = Mathf.Max(health - damage, 0f);

        if (Mathf.Approximately(health, 0f)) { 
            Die();
        }
    }

    [Server]
    protected virtual void Die()
    {
        // Debug.Log(gameObject.name + " died");
        OnDie.Invoke();
        NetworkServer.Destroy(gameObject);  
        // Debug.Log("Destroyed " + gameObject.name);
    }

    [Server]
    protected void SetHealth(float health)
    {
        this.health = health;
        if (this.health <= 0) { Die(); }
        else if (this.health > maxHealth) { this.health = maxHealth; }
    }

    [Server]
    protected void SetMaxHealth(float maxHealth)
    {
        if(maxHealth <= 0) { 
            Debug.LogError("Max health cannot be less than or equal to 0"); 
            return; 
        } 
        this.maxHealth = maxHealth;
        if (health > this.maxHealth) { health = this.maxHealth; } 
    }

    #endregion
}
