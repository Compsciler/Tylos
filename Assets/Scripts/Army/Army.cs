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
    [SyncVar] private Army convertArmy = null; // Cache the Army component of the convert target

    // MonoBehaviour references
    ArmyVisuals armyVisuals;
    ArmyHealth armyHealth;
    ArmyConversion armyConversion;
    public ArmyConversion ArmyConversion => armyConversion;
    #endregion

    private const float ConversionRateAbsorb = 0.3f;
    private const float ConversionRateIdle = 0.02f;
    #region Events
    public static event Action<Army> ServerOnArmySpawned;
    public static event Action<Army> ServerOnArmyDespawned;

    public static event Action<Army> AuthorityOnArmySpawned;
    public static event Action<Army> AuthorityOnArmyDespawned;

    // the mean of the army identity on complex plane
    // this is calculated on on unit add/remove
    private Vector3 _meanC;
    private Vector2 _meanZ;
    private float _deviance;
    
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
        armyConversion = GetComponent<ArmyConversion>();
    }

    void Update()
    {
        if (isServer) // Handle game logic
        {
            switch (state)
            {
                case ArmyState.Attacking:
                    Attack();
                    break;
                case ArmyState.Converting:
                    Convert();
                    break;
                default:
                    break;
            }
        }

        if (isClient) // Handle client visuals
        {
            switch (state)
            {
                case ArmyState.Attacking:
                    if (attackTarget != null && IsInRange(attackTarget.transform.position, attackRange))
                        armyVisuals.DrawDeathRay(attackTarget.transform.position);
                    break;
                case ArmyState.Converting:
                    if (convertTarget != null && IsInRange(convertTarget.transform.position, attackRange))
                        // TODO: Draw convert ray
                        armyVisuals.DrawDeathRay(convertTarget.transform.position);
                    break;
            }
        }
    }

    private void FixedUpdate()
    {
        _armyUnitsLocal.Clear();

        foreach (var u in armyUnits)
        {
            _armyUnitsLocal.Add(u.Clone());
        }
        
        // recalculate mean
        // there is no point doing this in the setter anymore
        // because all the list flushing already threw efficiency out of the window
        _armyComplex = ArmyUnits.Select(i => i.GetIdentityZ()).ToList();
        
        _meanC = Vector3.zero;
        _meanZ = Vector2.zero;
        
        foreach (var u in _armyUnitsLocal)
        {
            _meanC += new Vector3(u.identityInfo.r, u.identityInfo.g,u.identityInfo.b);
        }

        foreach (var z in _armyComplex)
        {
            _meanZ += z;
        }

        _meanC /= _armyUnitsLocal.Count;
        _meanZ /= _armyUnitsLocal.Count;
        
        
        // update the army's visual color
        _armyIdentity.SetIdentity(_meanC.x, _meanC.y, _meanC.z);

        ProcessMeanIdentityShift(_meanC, ConversionRateIdle * Time.fixedDeltaTime);
        // flush the synced list
        armyUnits.Clear();
        foreach (var u in _armyUnitsLocal)
        {
            armyUnits.Add(u);
        }

        _deviance = CalculateDeviance();
    }
    #endregion

    #region Unit Operations
    public void AddUnit(Unit unit)
    {
        _armyUnitsLocal.Add(unit);
    }
    
    public void Absorb(SyncList<Unit> units)
    {
        ArmyUnits.AddRange(units);
        // I really don't want to calculate this multiple times per frame
        // ideally this is refactored into IdentityInfo
        _armyUnitsLocal.Clear();

        foreach (var u in armyUnits)
        {
            _armyUnitsLocal.Add(u.Clone());
        }

        // recalculate mean
        // there is no point doing this in the setter anymore
        // because all the list flushing already threw efficiency out of the window
        var meanC = Vector3.zero;
        foreach (var u in _armyUnitsLocal)
        {
            meanC += new Vector3(u.identityInfo.r, u.identityInfo.g,u.identityInfo.b);
        }
        meanC /= _armyUnitsLocal.Count;

        // var mean = Vector2.zero;
        // foreach (var z in _armyComplex)
        // {
        //     mean += z;
        // }
        // mean /= _armyComplex.Count;
        // Debug.Log(mean);
        
        // update the army's visual color
        _armyIdentity.SetIdentity(meanC.x, meanC.y, meanC.z);

        ProcessMeanIdentityShift(meanC, ConversionRateAbsorb);
        // flush the synced list
        armyUnits.Clear();
        foreach (var u in _armyUnitsLocal)
        {
            armyUnits.Add(u);
        }
    }
    #endregion

    #region ColorIdentity 

    private void ProcessMeanIdentityShift(Vector3 meanC, float step)
    {
        for (int i = 0; i < _armyUnitsLocal.Count; i++)
        {
            // this is the hsv identity of the unit we are currently dealing with
            var identity = _armyUnitsLocal[i].identityInfo;
            var originalColor = new Vector3(identity.r, identity.g, identity.b);
            
            var deltaNewColor = (meanC - originalColor) * step;
            var newColor = originalColor + deltaNewColor;

            var newUnit = _armyUnitsLocal[i].Clone();

            newUnit.identityInfo.r = newColor.x;
            newUnit.identityInfo.g = newColor.y;
            newUnit.identityInfo.b = newColor.z;

            _armyUnitsLocal[i] = newUnit;
        }
    }



    private float CalculateDeviance()
    {
        var armyIdentityColors = _armyUnitsLocal.Select
            (i => new Vector3(i.identityInfo.r, i.identityInfo.g, i.identityInfo.b)).ToList();
        if (armyIdentityColors.Count == 0)
        {
            return 0f;
        }
        var mean = _meanC;
        // using squared magnitude because it's like a standard
        // PCA uses another three stdev metric
        // is there a way to unify them to reduce problems?
        var stdev = armyIdentityColors.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyIdentityColors.Count;
        return stdev;
    }

    public float GetDeviance()
    {
        return _deviance;
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
            if (IsInRange(attackTarget.transform.position, attackRange))
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
        convertArmy = entity.GetComponent<Army>();
        SetState(ArmyState.Converting);
    }

    [Server]
    private void Convert()
    {
        if (convertTarget == null)
        {
            state = ArmyState.Idle;
            return;
        }
        else
        {
            if (IsInRange(convertTarget.transform.position, attackRange))
            {
                entityMovement.Stop();
                if (convertArmy == null)
                {
                    Debug.LogError("Convert army is null");
                }
                else if (convertArmy.ArmyConversion == null)
                {
                    Debug.LogError("Convert army has no conversion component");
                }
                convertArmy.ArmyConversion.Convert(this);
            }
            else
            {
                entityMovement.Move(convertTarget.transform.position);
            }
        }
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

    [Client]
    public override void TryConvert(Entity entity)
    {
        if (!isOwned || entity == null || entity.GetComponent<Army>() == null) { return; }
        CmdConvert(entity);
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
            armyConversion.SetResistance(ArmyUnits.Count);
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

    private bool IsInRange(Vector3 targetPosition, float range)
    {
        Vector3 offset = targetPosition - transform.position;
        float distance = offset.magnitude;
        float trueAttackRange = attackRange + transform.lossyScale.x + attackTarget.transform.lossyScale.x; // Takes into account the size of the army and the target

        return (distance <= trueAttackRange);
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
