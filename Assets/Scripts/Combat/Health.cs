using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [SerializeField] float maxHealth = 1f;

    [SyncVar(hook = nameof(HandleHealthUpdated))]
    float currentHealth;

    public event Action ServerOnDie;

    public event Action<float, float> ClientOnHealthUpdated;

    #region Server

    public override void OnStartServer()
    {
        currentHealth = maxHealth;
    }

    [Server]
    public void DealDamage(int damageAmount)
    {
        if (Mathf.Approximately(currentHealth, 0f)) { return; }

        currentHealth = Mathf.Max(currentHealth - damageAmount, 0f);

        if (Mathf.Approximately(currentHealth, 0f)) { 
            ServerOnDie?.Invoke();
        }
    }

    #endregion

    #region Client

    private void HandleHealthUpdated(float oldHealth, float newHealth)
    {
        ClientOnHealthUpdated?.Invoke(newHealth, maxHealth);
    }

    #endregion
}
