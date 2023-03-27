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

    [Header("Army Settings")]
    [SerializeField] private GameObject armyPrefab = null;
    [SerializeField] private Transform armySpawnPoint = null;

    [Header("Base Settings")]
    // Base settings
    [SerializeField] 
    [Tooltip("How many seconds between each unit spawn")]
    [Range(1f, 60f)]
    private float spawnRate = 5f;

    // Internal variables
    private readonly SyncList<Unit> _baseUnits = new SyncList<Unit>();
    private IdentityInfo _baseIdentityInfo; 
    private BaseSpawner _baseSpawner;

    #region Server
    public override void OnStartServer()
    {
        ServerOnBaseSpawned?.Invoke(this);
        _baseIdentityInfo = GetComponent<ObjectIdentity>().Identity;
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
            Unit unit = new Unit(_baseIdentityInfo);
            _baseUnits.Add(unit);

            yield return new WaitForSeconds(spawnRate);
        }
    }

    #endregion

    #region Client
    public override void OnStartAuthority()
    {
        AuthorityOnBaseSpawned?.Invoke(this);
    }

    public override void OnStopClient()
    {
        if (!isOwned) { return; }

        AuthorityOnBaseDespawned?.Invoke(this);
    }

    public int GetBaseUnitCount()
    {
        return _baseUnits.Count;
    }

    public void AddUnitToBase(Unit unit)
    {
        _baseUnits.Add(unit);
    }

    [Client]
    public override void TryMove(Vector3 position) // When move command is issued to the base, spawn an army and move it to the position
    {
        if (!isOwned || _baseUnits.Count == 0) { return; } // If there are no units in the base, don't do anything

        Unit[] units = new Unit[_baseUnits.Count]; // Convert to array since SyncList doesn't work with Mirror Command
        _baseUnits.CopyTo(units, 0);
        _baseSpawner.CmdSpawnMoveArmy(units, position);
        _baseUnits.Clear();
    }
    

    #endregion
}
