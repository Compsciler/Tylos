using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Unity.VisualScripting;

[RequireComponent(typeof(ArmyMovement))]
public class Army : Entity
{
    readonly SyncList<Unit> armyUnits = new SyncList<Unit>(); // Change to set if necessary
    
    // the mean of the army identity on complex plane
    // this is calculated on on unit add/remove
    private Vector2 _meanZ;
    // the complex version of the armies
    // TODO: please refactor this to be a part of identity info
    // this is updated per frame
    private List<Vector2> _armyComplex;

    // this might need to be changed
    private float _identityChangeRate = 0.2f;

    public ReadOnlyCollection<Unit> ArmyUnits => new ReadOnlyCollection<Unit>(armyUnits);
    
    public Army() { }

    public void AddUnit(Unit unit)
    {
        var z = CalculateIdentityComplex(unit);
        if (armyUnits.Count == 0)
        {
            _meanZ = z;
        }
        else
        {
            _meanZ = _meanZ * (float) armyUnits.Count + z;
            _meanZ /= (armyUnits.Count + 1);
        }
        
        armyUnits.Add(unit);
    }
    
    public void RemoveUnit(Unit unit)
    {
        var z = CalculateIdentityComplex(unit);
        if (armyUnits.Count == 0)
        {
            _meanZ = Vector2.zero;
        }
        else
        {
            _meanZ = _meanZ * (float) armyUnits.Count - z;
            _meanZ /= (armyUnits.Count - 1);
        }
        armyUnits.Remove(unit);
    }
    
    public void SetUnits(Unit[] units) // Use array because Mirror doesn't support lists in commands 
    {
        armyUnits.Clear();
        _meanZ = Vector2.zero;
        foreach(Unit unit in units)
        {
            AddUnit(unit);
        }
    }

    private void FixedUpdate()
    {
        // I really don't want to calculate this multiple times per frame
        // ideally this is refactored into IdentityInfo
        _armyComplex = IdentityComplex();
        
        // this is the mean H that all others will tend towards
        var meanAngle = Mathf.Atan2(_meanZ.y, _meanZ.x);
        for (int i = 0; i < armyUnits.Count; i++)
        {
            // this is the hsv identity of the unit we are currently dealing with
            var identity = armyUnits[i].IdentityInfo;
            Color.RGBToHSV(new Color(identity.r, identity.g, identity.b), out var h, out var s, out var v);

            // this is fine?
            // if both y and x are zero
            // it just gives 0, which should have not effect in the subsequent calculation anyways
            var angle = Mathf.Atan2(_armyComplex[i].y, _armyComplex[i].x);
            
            var newAngle = Mathf.MoveTowardsAngle(angle, meanAngle, _identityChangeRate * Time.fixedDeltaTime);
            // an awkward way to compress it into 0-1
            const float twoPI = (Mathf.PI * 2);
            var newH = ((newAngle + twoPI) % twoPI)/ twoPI;

            var newColor = Color.HSVToRGB(newH, s, v);
            armyUnits[i].IdentityInfo = new IdentityInfo(newColor);
        }
    }
    
    

    public float GetDeviance()
    {
        var armyComplex = _armyComplex;
        var mean = _meanZ;
        // using squared magnitude because it's like a standard
        // PCA uses another three stdev metric
        // is there a way to unify them to reduce problems?
        var stdev = armyComplex.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyComplex.Count;
        return stdev;
    }

    // this function does a PCA on the units identites
    // and splits the army along the principle axis
    // the split is returned as a tuple of lists
    public (List<Unit>, List<Unit>) CalculateSplit()
    {
        var armyComplex = _armyComplex;
        
        // GetEigenCentroid returns (eigenvector, centroid) of the dataset
        var (eigenVector, centroid) = GetEigenCentroid(armyComplex, _meanZ);
        
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
            armyComplex.Add(
                CalculateIdentityComplex(u)
                );
        }

        return armyComplex;
    }

    private Vector2 CalculateIdentityComplex(Unit u)
    {
        var identity = u.IdentityInfo;
        var rgbIdentity = new Color(identity.r, identity.g, identity.b);
        float h;
        Color.RGBToHSV(rgbIdentity, out h, out _, out _);
        var angle = 2 * Mathf.PI * h;
        var complex = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        return complex;
    }
    
    // this function calculates the eigenvector as well as the centroid
    private (Vector2, Vector2) GetEigenCentroid(List<Vector2> data, Vector2 meanZ)
    {
        var xMean = meanZ.x;
        var yMean = meanZ.y;
        
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

    public static event Action<Army> ServerOnArmySpawned;
    public static event Action<Army> ServerOnArmyDespawned;

    public static event Action<Army> AuthorityOnArmySpawned;
    public static event Action<Army> AuthorityOnArmyDespawned;

    void Awake()
    {
        entityMovement = GetComponent<ArmyMovement>();
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
    #endregion
}
