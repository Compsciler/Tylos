using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Unity.VisualScripting;
using UnityEngine.AI;

[RequireComponent(typeof(ArmyMovement), typeof(ArmyHealth), typeof(ArmyVisuals))]
public class Army : Entity
{
    # region variables
    // State variables
    [SyncVar] private ArmyState state = ArmyState.Idle;
    public ArmyState State => state;

    // Unit variables
    readonly SyncList<Unit> armyUnits = new SyncList<Unit>(); // Change to set if necessary
    public SyncList<Unit> ArmyUnits => armyUnits;
    // this is our local army units copy
    // the one above will be flushed every single frame
    public List<Unit> _armyUnitsLocal = new();

    const float twoPI = (Mathf.PI * 2);

    // Attack variables
    [Header("Attack settings")]
    [SerializeField] const float minUnitAttackDamage = 1f;
    [SerializeField] const float maxUnitAttackDamage = 2f;
    [SyncVar] private float attackDamage = 0f;
    [SyncVar][SerializeField] private float attackRange = 5f;
    [SyncVar] private Entity attackTarget = null; // Only used on the server

    // Convert variables
    [Header("Convert settings")]
    [SyncVar] private Entity convertTarget = null; // Only used on the server    

    // MonoBehaviour references
    ArmyVisuals armyVisuals;
    ArmyHealth armyHealth;
    #endregion

    #region Events
    public static event Action<Army> ServerOnArmySpawned;
    public static event Action<Army> ServerOnArmyDespawned;

    public static event Action<Army> AuthorityOnArmySpawned;
    public static event Action<Army> AuthorityOnArmyDespawned;

    // the mean of the army identity on complex plane
    // this is calculated on on unit add/remove
    private Vector2 _meanZ;
    // the complex version of the armies
    // TODO: please refactor this to be a part of identity info
    // this is updated per frame
    private List<Vector2> _armyComplex;

    private ObjectIdentity _armyIdentity;

    private float _meanAngle;

    // this might need to be changed
    private float _identityChangeRate = 2f;

    public static event Action<Army> AuthorityOnArmySelected;
    public static event Action<Army> AuthorityOnArmyDeselected;

    #endregion
    public Army() { }

    #region Unity Methods
    void Awake()
    {
        entityMovement = GetComponent<ArmyMovement>();
        entityHealth = GetComponent<ArmyHealth>();
        armyHealth = entityHealth as ArmyHealth;
        if (armyHealth == null)
            Debug.LogError("ArmyHealth is null");
        armyVisuals = GetComponent<ArmyVisuals>();
        _armyIdentity = GetComponent<ObjectIdentity>();
    }

    void Update()
    {
        if (isServer) // Handle game logic
        {
            if (state == ArmyState.Attacking)
            {
                Attack();
            }
        }

        if (isClient) // Handle client visuals
        {
            if (state == ArmyState.Attacking && attackTarget != null)
            {
                if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
                {
                    armyVisuals.DrawDeathRay(attackTarget.transform.position);
                }
            }

        }
    }

    private void FixedUpdate()
    {
        // I really don't want to calculate this multiple times per frame
        // ideally this is refactored into IdentityInfo
        _armyUnitsLocal.Clear();

        foreach (var u in armyUnits)
        {
            _armyUnitsLocal.Add(u.Clone());
        }
        // flush the complex version list
        _armyComplex = IdentityComplex();

        // recalculate mean
        // there is no point doing this in the setter anymore
        // because all the list flushing already threw efficiency out of the window

        _meanZ = Vector2.zero;
        foreach (var z in _armyComplex)
        {
            _meanZ += z;
        }
        _meanZ /= _armyComplex.Count;

        // var mean = Vector2.zero;
        // foreach (var z in _armyComplex)
        // {
        //     mean += z;
        // }
        // mean /= _armyComplex.Count;
        // Debug.Log(mean);


        // update the army's visual color
        _meanAngle = Mathf.Atan2(_meanZ.y, _meanZ.x);
        var meanH = ((_meanAngle + twoPI) % twoPI) / twoPI;
        var meanColor = Color.HSVToRGB(meanH, 1f, 1f);
        _armyIdentity.SetIdentity(meanColor.r, meanColor.g, meanColor.b);

        ProcessMeanIdentityShift();
        // flush the synced list
        armyUnits.Clear();
        foreach (var u in _armyUnitsLocal)
        {
            armyUnits.Add(u);
        }

    }
    #endregion

    #region Unit Operations
    public void AddUnit(Unit unit)
    {
        _armyUnitsLocal.Add(unit);
    }

    public void AddUnits(List<Unit> units)
    {
        _armyUnitsLocal.AddRange(units);
    }

    public void RemoveUnit(Unit unit)
    {
        _armyUnitsLocal.Remove(unit);
    }

    public float GetAttackDamage()
    {
        return attackDamage;
    }


    public void SetUnits(Unit[] units) // Use array because Mirror doesn't support lists in commands 
    {
        armyUnits.Clear();
        foreach (Unit unit in units)
        {
            AddUnit(unit);
        }
    }
    #endregion

    #region ColorIdentity 

    private void ProcessMeanIdentityShift()
    {
        for (int i = 0; i < _armyUnitsLocal.Count; i++)
        {
            // this is the hsv identity of the unit we are currently dealing with
            var identity = _armyUnitsLocal[i].identityInfo;
            var originalColor = new Color(identity.r, identity.g, identity.b);
            Color.RGBToHSV(originalColor, out var h, out var s, out var v);

            // this is fine?
            // if both y and x are zero
            // it just gives 0, which should have not effect in the subsequent calculation anyways
            var angle = Mathf.Atan2(_armyComplex[i].y, _armyComplex[i].x);

            // for whatever reason this shit operates in degrees
            // so I will have to convert to deg and convert it back
            var newAngle = Mathf.MoveTowardsAngle
                (
                    angle / Mathf.PI * 180f,
                    _meanAngle / Mathf.PI * 180f,
                    _identityChangeRate * Time.fixedDeltaTime
                );

            newAngle = newAngle / 180f * Mathf.PI;

            // an awkward way to compress it into 0-1

            var newH = ((newAngle + twoPI) % twoPI) / twoPI;

            var newColor = Color.HSVToRGB(newH, s, v);


            var newUnit = _armyUnitsLocal[i].Clone();

            newUnit.identityInfo.r = newColor.r;
            newUnit.identityInfo.g = newColor.g;
            newUnit.identityInfo.b = newColor.b;

            _armyUnitsLocal[i] = newUnit;
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
        foreach (var u in _armyUnitsLocal)
        {
            var identity = u.identityInfo;
            var rgbIdentity = new Color(identity.r, identity.g, identity.b);
            float h;
            Color.RGBToHSV(rgbIdentity, out h, out _, out _);
            //0->0, 1->2pi
            var angle = 2 * Mathf.PI * h;
            var complex = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            armyComplex.Add(u.GetIdentityZ());
        }

        return armyComplex;
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

    #region Server

    public override void OnStartServer()
    {
        ServerOnArmySpawned?.Invoke(this);
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
        armyVisuals.SetScale(armyUnits.Count);
    }

    [Server]
    private void InitializeArmyStats()
    {
        attackDamage = ArmyUtils.CalculateAttackPower(ArmyUnits, minUnitAttackDamage, maxUnitAttackDamage);
    }

    [Server]
    public SyncList<Unit> GetArmyUnits()
    {
        return armyUnits;
    }


    /// <summary>
    /// Makes sure attackTarget is set to null when it dies
    /// </summary>
    [Server]
    private void HandleAttackTargetOnDie()
    {
        attackTarget = null;
        state = ArmyState.Idle;
    }

    [Server]
    private void SetState(ArmyState state)
    {
        this.state = state;
        if (state != ArmyState.Attacking)
        { // Reset the attack target if we are not attacking
            attackTarget = null;
        }
    }

    [Command]
    private void CmdSetState(ArmyState state)
    {
        SetState(state);
    }

    /// <summary>
    /// Sets the attack target and state to attacking
    /// </summary>
    /// <param name="entity"></param>
    [Command]
    private void CmdAttack(Entity entity)
    {
        if (entity == null) { return; }
        if (entity.EntityHealth == null)
        {
            Debug.LogError("Entity has no health component");
            return;
        }
        attackTarget = entity;
        attackTarget.GetComponent<EntityHealth>().OnDie.AddListener(HandleAttackTargetOnDie);
        SetState(ArmyState.Attacking);
    }

    /// <summary>
    /// Called in the update loop when the army is attacking
    /// </summary>
    [Server]
    private void Attack()
    {
        if (attackTarget == null) // Target died
        {
            // Debug.Log("Target died");
            state = ArmyState.Idle;
        }
        else
        {
            if (Vector3.Distance(transform.position, attackTarget.transform.position) <= attackRange)
            {
                entityMovement.Stop();
                attackTarget.EntityHealth.TakeDamage(attackDamage * Time.deltaTime);
            }
            else
            {
                entityMovement.Move(attackTarget.transform.position);
            }
        }
    }

    [Command]
    public void CmdConvert(Entity entity)
    {
        if (entity == null) { return; }
        if (entity.GetComponent<Army>() == null)
        {
            return;
        }
        convertTarget = entity;
    }
    #endregion

    #region Client
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (ArmyUnits == null || armyVisuals == null)
        {
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

    private void OnArmyUnitsUpdated(SyncList<Unit>.Operation op, int index, Unit oldUnit, Unit newUnit)
    {
        if (isServer)
        {
            if (armyVisuals == null)
            {
                return;
            }
            else
            {
                if (armyVisuals.Count != armyUnits.Count)
                { // If the army visuals count doesn't match the army units count, update the count and visuals
                    armyVisuals.SetScale(armyUnits.Count);
                    attackDamage = ArmyUtils.CalculateAttackPower(ArmyUnits, minUnitAttackDamage, maxUnitAttackDamage);
                }
            }
        }
        else if (isClient)
        {
            armyVisuals.SetColor(ArmyUnits); // TODO: Optimize this so the entire list doesn't have to be passed
        }
    }

    [Client]
    public void InvokeArmySelectEvents(bool selected)
    {
        if (selected)
        {
            AuthorityOnArmySelected?.Invoke(this);
        }
        else
        {
            AuthorityOnArmyDeselected?.Invoke(this);
        }
    }
    #endregion
}

public enum ArmyState
{
    Idle,
    Moving,
    Attacking,
    Converting
}
