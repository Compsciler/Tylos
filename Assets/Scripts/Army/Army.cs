using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

[RequireComponent(typeof(ArmyMovement), typeof(ArmyHealth), typeof(ArmyVisuals))]
public class Army : Entity
{

    [Header("Army visual settings")]
    [SerializeField]
    [Range(0.1f, 10f)]
    private float defaultScale = 1f;

    [SerializeField]
    [Range(0.1f, 2f)]
    private float scaleIncrementPerUnit = 1f;

    // State variables
    [SyncVar] private ArmyState state = ArmyState.Idle;
    public ArmyState State => state;

    // Unit variables
    readonly SyncList<Unit> armyUnits = new SyncList<Unit>(); // Change to set if necessary
    public SyncList<Unit> ArmyUnits => armyUnits;


    // Attack variables
    [Header("Attack settings")]
    [SyncVar] private float attackDamage = 0f;
    [SyncVar][SerializeField] private float attackRange = 5f;
    [SyncVar] private Entity attackTarget = null; // Only used on the server

    // MonoBehaviour references
    ArmyVisuals armyVisuals;
    ArmyHealth  armyHealth;

    public Army() { }

    void Awake()
    {
        entityMovement = GetComponent<ArmyMovement>();
        entityHealth = GetComponent<ArmyHealth>();
        armyHealth = entityHealth as ArmyHealth;
        if(armyHealth == null)
            Debug.LogError("ArmyHealth is null"); 
        armyVisuals = GetComponent<ArmyVisuals>();
    }

    void Update()
    {
        if (isServer) // Handle game logic
        {
            
            if (state == ArmyState.Attacking)
            {
                if (attackTarget == null) // Target died
                {
                    Debug.Log("Target died");
                    state = ArmyState.Idle;
                }
                else
                {
                    if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
                    {
                        Debug.Log("Attacking target");
                        entityMovement.Stop();
                        attackTarget.EntityHealth.TakeDamage(attackDamage * Time.deltaTime);
                    }
                    else
                    {
                        Debug.Log("Target out of range");
                        entityMovement.Move(attackTarget.transform.position);
                    }
                }
            }
        }

        if (isClient) // Handle client visuals
        {
            if (state == ArmyState.Attacking)
            {
                if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
                {
                    armyVisuals.DrawDeathRay(attackTarget.transform.position);
                }
            }

        }
    }
    public void AddUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }
    public void RemoveUnit(Unit unit)
    {
        armyUnits.Remove(unit);
    }

    public float GetAttackDamage()
    {
        return attackDamage;
    }

    #region ColorIdentity 

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

        for (int i = 0; i < armyComplex.Count; i++)
        {

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
            var identity = u.identityInfo;
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
        var l1 = (varX + varY + delta) / 2;
        var l2 = Mathf.Abs(varX + varY - delta) / 2;

        var l = Mathf.Max(l1, l2);
        // the new thing to solve is then
        // [varX - l, covXY; covXY, varY - l][a; b] = 0

        var aFactor = varX - l + covXY;
        var bFactor = covXY + varY - l;

        var eigenVector = new Vector2(bFactor, -aFactor).normalized;
        return (eigenVector, new Vector2(xMean, yMean));
    }

    #endregion

    #region Events
    public static event Action<Army> ServerOnArmySpawned;
    public static event Action<Army> ServerOnArmyDespawned;

    public static event Action<Army> AuthorityOnArmySpawned;
    public static event Action<Army> AuthorityOnArmyDespawned;

    #endregion

    #region Server

    public override void OnStartServer()
    {
        ServerOnArmySpawned?.Invoke(this);
        UpdateScale();
        InitializeArmyStats();
    }

    public override void OnStopServer()
    {
        ServerOnArmyDespawned?.Invoke(this);
    }

    [Server]
    public void SetUnits(IdentityInfo identity, int count) // Use array because Mirror doesn't support lists in commands 
    {
        armyUnits.Clear();
        // add count number of units with the given identity to the army
        for (int i = 0; i < count; i++)
        {
            armyUnits.Add(new Unit(identity));
        }
        UpdateScale();
    }

    [Server]
    private void UpdateScale()
    {
        Vector3 start = gameObject.transform.localScale;
        Vector3 end = (Vector3.one * defaultScale) + (Vector3.one * scaleIncrementPerUnit * armyUnits.Count);
        gameObject.transform.localScale = end;
    }

    [Server]
    private void InitializeArmyStats()
    {
        attackDamage = ArmyUtils.CalculateAttackPower(ArmyUnits);
    }

    [Server]
    public SyncList<Unit> GetArmyUnits()
    {
        return armyUnits;
    }
    
    [Command]
    private void CmdSetState(ArmyState state)
    {
        this.state = state;
    }

    [Command]
    private void CmdAttack(Entity entity)
    {
        if (entity == null) { return; }
        if (entity.EntityHealth == null)
        {
            Debug.LogError("Entity has no health component");
            return;
        }
        Debug.Log("Attacking");
        attackTarget = entity;
        state = ArmyState.Attacking;
    }
    #endregion

    #region Client
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (ArmyUnits == null)
        {
            Debug.LogError("Army units is null");
            return;
        }
        else if (armyVisuals == null)
        {
            Debug.LogError("Army visuals is null");
            return;
        }

        armyUnits.Callback += OnArmyUnitsUpdated;
        armyVisuals.SetColor(ArmyUnits); // Initialize the color of the army
    }

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
    public override void TryMove(Vector3 position)
    {
        if (!isOwned) { return; }

        base.TryMove(position); // This does the actual movement
        CmdSetState(ArmyState.Moving);
    }

    [Client]
    public override void TryAttack(Entity entity)
    {
        if (!isOwned || entity == null) { return; }

        CmdAttack(entity);
    }

    [Client]
    private void OnArmyUnitsUpdated(SyncList<Unit>.Operation op, int index, Unit oldUnit, Unit newUnit)
    {
        armyVisuals.SetColor(ArmyUnits); // TODO: Optimize this so the entire list doesn't have to be passed
    }
    #endregion
}

public enum ArmyState
{
    Idle,
    Moving,
    Attacking
}