using System;
using System.Collections;
using System.Collections.ObjectModel;
using Mirror;
using UnityEngine;

/// <summary>
/// 
/// </summary>
[RequireComponent(typeof(ObjectIdentity), typeof(BaseSpawner))]
public class Base : Entity
{
    // Events 
    public static event Action<Base> ServerOnBaseSpawned;
    public static event Action<Base> ServerOnBaseDespawned;

    public static event Action<Base> AuthorityOnBaseSpawned;
    public static event Action<Base> AuthorityOnBaseDespawned;

    [SerializeField]
    [Tooltip("How many seconds between each unit spawn")]
    [Range(0f, 60f)]
    private float spawnRate = 5f;

    // Internal variables
    [SyncVar] private int _baseUnitCount = 0;
    [SyncVar] private IdentityInfo _baseIdentityInfo;
    private BaseSpawner _baseSpawner;

    private void Awake() {
        entityHealth = GetComponent<BaseHealth>();
    }

    #region Server
    public override void OnStartServer()
    {
        _baseIdentityInfo = GetComponent<ObjectIdentity>().Identity;
        ServerOnBaseSpawned?.Invoke(this);
        _baseSpawner = GetComponent<BaseSpawner>();
        StartCoroutine(AddUnits());
    }

    public override void OnStopServer()
    {
        ServerOnBaseDespawned?.Invoke(this);
    }

    [Server]
    private IEnumerator AddUnits()
    {
        while (true)
        {
            _baseUnitCount++;
            yield return new WaitForSeconds(spawnRate);
        }
    }

    [Command]
    private void CmdClearBaseUnits()
    {
        _baseUnitCount = 0;
    }
    #endregion

    #region Client
    public override void OnStartAuthority()
    {
        _baseSpawner = GetComponent<BaseSpawner>();
        AuthorityOnBaseSpawned?.Invoke(this);
    }

    public override void OnStopClient()
    {
        if (!isOwned) { return; }

        AuthorityOnBaseDespawned?.Invoke(this);
    }

    public int GetBaseUnitCount()
    {
        return _baseUnitCount;
    }

    public float GetBaseHealth()
    {
        return entityHealth.Health;
    }

    [Client]
    public override void TryMove(Vector3 position) // When move command is issued to the base, spawn an army and move it to the position
    {
        if (!isOwned || _baseUnitCount == 0) { return; } // If there are no units in the base, don't do anything
        
        _baseSpawner.CmdSpawnMoveArmy(_baseIdentityInfo, _baseUnitCount, position);
        CmdClearBaseUnits();
    }


    #endregion
}
