using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using System;

public class Army : NetworkBehaviour
{
    List<Unit> armyUnits = new List<Unit>();  // Change to set if necessary
    public ReadOnlyCollection<Unit> ArmyUnits => armyUnits.AsReadOnly();
    public float GetDeviance() {
        // complex sum to find centroid
        var sum = Vector2.zero;
        var armyComplex = new List<Vector2>();
        foreach (var u in armyUnits)
        {
            var identity = u.ObjectIdentity.Identity;
            var rgbIdentity = new Color(identity.r, identity.g, identity.b);
            float h;
            Color.RGBToHSV(rgbIdentity, out h, out _, out _);
            //0->0, 1->2pi
            var angle = 2 * Mathf.PI * h;
            var complex = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            armyComplex.Add(complex);
        }
        sum = armyComplex.Aggregate(sum, (current, c) => current + c);
        var mean = sum / armyComplex.Count;
        // using squared magnitude because it's like a standard
        var stdev = armyComplex.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyComplex.Count;
        return stdev;
    }
    public Army() {}

    public void AddUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }
    public void RemoveUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }

    [SerializeField] UnityEvent onSelected;
    [SerializeField] UnityEvent onDeselected;

    ArmyMovement armyMovement;
    public ArmyMovement UnitMovement_ => armyMovement;

    public static event Action<Army> ServerOnArmySpawned;
    public static event Action<Army> ServerOnArmyDespawned;

    public static event Action<Army> AuthorityOnArmySpawned;
    public static event Action<Army> AuthorityOnArmyDespawned;

    void Awake()
    {
        armyMovement = GetComponent<ArmyMovement>();
    }

    #region Server

    public override void OnStartServer()
    {
        ServerOnArmySpawned?.Invoke(this);
    }

    public override void OnStopServer()
    {
        ServerOnArmyDespawned?.Invoke(this);
    }

    #endregion

    #region Client

    public override void OnStartAuthority()
    {
        AuthorityOnArmySpawned?.Invoke(this);
    }

    public override void OnStopClient()
    {
        if (!isOwned) { return; }

        AuthorityOnArmyDespawned?.Invoke(this);
    }

    [Client]
    public void Select()
    {
        if (!isOwned) { return; }  // Change for dev mode, check may also be redundant from UnitSelectionHandler

        onSelected?.Invoke();
    }

    [Client]
    public void Deselect()
    {
        if (!isOwned) { return; }

        onDeselected?.Invoke();
    }
    #endregion
}
