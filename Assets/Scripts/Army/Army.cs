using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Unity.Mathematics;


using UnityEngine;
using UnityEngine.Events;
using Mirror;
using System;

public class Army : NetworkBehaviour
{
    List<Unit> armyUnits = new List<Unit>(); // Change to set if necessary
    public ReadOnlyCollection<Unit> ArmyUnits => armyUnits.AsReadOnly();
    
    public Army() { }

    public void AddUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }
    public void RemoveUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }
    
    public float GetDeviance()
    {
        // complex sum to find centroid
        var sum = Vector2.zero;
        var armyComplex = IdentityComplex();

        sum = armyComplex.Aggregate(sum, (current, c) => current + c);
        var mean = sum / armyComplex.Count;
        // using squared magnitude because it's like a standard
        var stdev = armyComplex.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyComplex.Count;
        return stdev;
    }

    // this function does a PCA on the units identites
    // and splits the army along the principle axis
    // the split is returned as a tuple of lists
    public (List<Unit>, List<Unit>) CalculateSplit()
    {
        var armyComplex = IdentityComplex();
        
        // GetEigenCentroid returns (eigenvector, centroid) of the dataset
        var (eigenVector, centroid) = GetEigenCentroid(armyComplex);
        
        // if we get a perfectly circular set it is possible for eigen to be 0
        // just randomly pick an axis if so
        if (eigenVector == Vector2.zero)
        {
            var angle = UnityEngine.Random.Range(0, Mathf.PI * 2);
            eigenVector = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
        
        // project the centroid onto the eigen
        var centroidProj = Vector2.Dot(centroid, eigenVector);

        var retSplit1 = new List<Unit>();
        var retSplit2 = new List<Unit>();

        for (int i = 0; i < armyComplex.Count; i++){
           
            if (Vector2.Dot(armyComplex[i], eigenVector) < centroidProj)
            {
                retSplit1.Add(armyUnits[i]);
            }
            else
            {
                retSplit2.Add(armyUnits[i]);
            }
        }
        
        return (retSplit1, retSplit2);
    }
    
    

    private List<Vector2> IdentityComplex()
    {
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

        return armyComplex;
    }
    
    // this function calculates the eigenvector as well as the centroid
    private (Vector2, Vector2) GetEigenCentroid(List<Vector2> data)
    {
        var xMean = 0f;
        xMean = data.Aggregate(xMean, (current, c) => current + c.x) / data.Count;
        var yMean = 0f;
        yMean = data.Aggregate(yMean, (current, c) => current + c.y) / data.Count;
        
        var varX = 0f;
        varX = data.Aggregate(varX, 
            (current, c) =>
            {
                return current + (c.x - xMean) * (c.x - xMean);
            }) / (data.Count - 1);
        
        var varY = 0f;
        varY = data.Aggregate(varY, 
            (current, c) =>
            {
                return current + (c.y - yMean) * (c.y - yMean);
            }) / (data.Count - 1);
        
        var covXY = 0f;
        covXY = data.Aggregate(covXY, 
            (current, c) =>
            {
                return current + (c.y - yMean) * (c.x - xMean);
            }) / (data.Count - 1);
        
        // the cov matrix is [varX, covXY; covXY, varY]
        // now we calculate the eigenvectors and eigenvalues
        
        var delta = math.sqrt((varX + varY) * (varX + varY) - 4 * (varX * varY - covXY * covXY));
        var l1 = (varX + varY + delta)/2;
        var l2 = Mathf.Abs(varX + varY - delta) / 2;

        var l = Mathf.Max(l1, l2);
        // the new thing to solve is then
        // [varX - l, covXY; covXY, varY - l][a; b] = 0

        var aFactor = varX - l + covXY;
        var bFactor = covXY + varY - l;

        var eigenVector = new Vector2(bFactor, -aFactor).normalized;
        return (eigenVector, new Vector2(xMean, yMean));
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
