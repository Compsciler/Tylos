using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(ObjectIdentity))]
public class Base : NetworkBehaviour
{
    // Events 
    public static event Action<Base> ServerOnBaseSpawned;
    public static event Action<Base> ServerOnBaseDespawned;

    public static event Action<Base> AuthorityOnBaseSpawned;
    public static event Action<Base> AuthorityOnBaseDespawned;

    // Base settings
    [SerializeField] 
    [Tooltip("How many seconds between each unit spawn")]
    [Range(1f, 60f)]
    private float spawnRate = 5f;

    // Internal variables
    private List<Unit> _baseUnits = new List<Unit>();
    private IdentityInfo _baseIdentityInfo; 

    #region Server
    public override void OnStartServer()
    {
        ServerOnBaseSpawned?.Invoke(this);
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
            Unit unit = new Unit();
            unit.ObjectIdentity.SetIdentity(_baseIdentityInfo);
            _baseUnits.Add(unit);

            yield return new WaitForSeconds(spawnRate);
        }
    }

    #endregion

    #region Client

    private void Awake()
    {
        _baseIdentityInfo = GetComponent<ObjectIdentity>().Identity;
    }

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
    

    #endregion
}
