using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ArmyMovement), typeof(ArmyHealth), typeof(ArmyVisuals))]
[RequireComponent(typeof(ArmyAudio))]
public class Army : Entity
{
    # region variables
    // State variables
    [SyncVar] private ArmyState state = ArmyState.Idle;
    public ArmyState State => state;

    // Unit variables
    readonly List<Unit> armyUnits = new(); // Change to set if necessary
    public List<Unit> ArmyUnits => armyUnits;

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

    [Header("Update settings")]
    [Tooltip("The interval in seconds between each color shift")]
    [Range(0.01f, 1f)]
    [SerializeField] private float colorShiftIntervalSeconds = 0.1f;

    // Visual Variables
    private Vector2 _meanZ;
    [SyncVar] private float deviance;
    public float Deviance => deviance;
    [SyncVar] private int size;
    public int Size => size;

    private float armySplitThreshold = 0.2f;

    // Dependencies
    ArmyVisuals armyVisuals;
    ArmyHealth armyHealth;
    ArmyConversion armyConversion;
    ArmyAudio armyAudio;
    public ArmyConversion ArmyConversion => armyConversion;
    #endregion

    private const float ConversionRateAbsorb = 0.3f;
    private const float ConversionRateIdle = 0.02f;
    [SerializeField] private GameObject unableToBuildIcon;
    [SerializeField] private GameObject buildingIcon;
    [SerializeField] private float iconDisplayDuration = 0.5f;

    #region Events
    public static event Action<Army> ServerOnArmySpawned;
    public static event Action<Army> ServerOnArmyDespawned;

    public static event Action<Army> AuthorityOnArmySpawned;
    public static event Action<Army> AuthorityOnArmyDespawned;

    // the complex version of the armies
    // TODO: please refactor this to be a part of identity info
    // this is updated per frame
    private List<Vector2> _armyComplex;

    public ObjectIdentity _armyIdentity;

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
        armyAudio = GetComponent<ArmyAudio>();
    }

    void Update()
    {
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
                    if (attackTarget != null && ArmyUtils.IsInRange(transform, attackTarget.transform, attackRange))
                    {
                        armyVisuals.DrawDeathRay(attackTarget.transform.position);
                        armyAudio.PlayAttackAudio();

                    }
                    else
                    {
                        armyAudio.StopAttackAudio();
                    }
                    break;
                case ArmyState.Converting:
                    if (convertTarget != null && ArmyUtils.IsInRange(transform, convertTarget.transform, attackRange))
                        // TODO: Draw convert ray
                        armyVisuals.DrawDeathRay(convertTarget.transform.position);
                    break;
                default:
                    armyAudio.StopAudio();
                    break;
            }
        }
    }

    #endregion

    #region Unit Operations
    [Server]

    public void Absorb(List<Unit> units)
    {
        ArmyUnits.AddRange(units);
        // recalculate mean
        _armyIdentity.SetIdentity(ArmyUtils.CalculateMeanColor(armyUnits));

        // Not sure why this is here, so I commented it out for now 
        // ProcessMeanIdentityShift(MeanColor, ConversionRateAbsorb); 
        ServerHandleArmyUnitsUpdated();
    }
    #endregion

    #region ColorIdentity 

    private void ProcessMeanIdentityShift(Vector3 meanC, float step)
    {
        for (int i = 0; i < armyUnits.Count; i++)
        {
            // this is the hsv identity of the unit we are currently dealing with
            var identity = armyUnits[i].identityInfo;
            var originalColor = new Vector3(identity.r, identity.g, identity.b);

            var deltaNewColor = (meanC - originalColor) * step;
            var newColor = originalColor + deltaNewColor;

            var newUnit = armyUnits[i].Clone();

            newUnit.identityInfo.r = newColor.x;
            newUnit.identityInfo.g = newColor.y;
            newUnit.identityInfo.b = newColor.z;

            armyUnits[i] = newUnit;
        }

        ServerHandleArmyUnitsUpdated();
    }

    #endregion

    #region Server

    public override void OnStartServer()
    {
        ServerOnArmySpawned?.Invoke(this);
        StartCoroutine(UnitsColorUpdate());
    }

    /// <summary>
    /// The main logic loop for the army's continuos color update
    /// </summary>
    /// <returns></returns>
    IEnumerator UnitsColorUpdate()
    {
        while (true)
        {
            _armyComplex = ArmyUtils.GetArmyComplex(armyUnits);
            _meanZ = ArmyUtils.CalculateMeanZ(_armyComplex);
            ProcessMeanIdentityShift(_armyIdentity.IdentityVec3, ConversionRateIdle * colorShiftIntervalSeconds);
            yield return new WaitForSeconds(colorShiftIntervalSeconds);
        }
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

        ServerHandleArmyUnitsUpdated();
    }

    [Server]
    public void KillUnit(Unit unit)
    {
        armyUnits.Remove(unit);
        ServerHandleArmyUnitsUpdated();
    }

    [Server]
    public void KillUnits(List<Unit> units)
    {
        foreach (var unit in units)
        {
            armyUnits.Remove(unit);
        }
        ServerHandleArmyUnitsUpdated();
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
            if (ArmyUtils.IsInRange(transform, attackTarget.transform, attackRange))
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
            if (ArmyUtils.IsInRange(transform, convertTarget.transform, attackRange))
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

    /// <summary>
    /// Updates all the SyncVars on the client
    /// </summary>
    [Server]
    public void ServerHandleArmyUnitsUpdated()
    {
        deviance = ArmyUtils.CalculateDeviance(ArmyUnits, _armyIdentity.IdentityVec3);
        size = armyUnits.Count;
        armyVisuals.SetScale(size);
        attackDamage = ArmyUtils.CalculateAttackPower(ArmyUnits, minUnitAttackDamage, maxUnitAttackDamage);
        armyConversion.SetResistance(ArmyUnits.Count);
        
        // 
        if (deviance > armySplitThreshold)
        {
            ArmySplitProcedure();
        }
    }

    private void ArmySplitProcedure()
    {
        var armies = CalculateSplit();
        armyUnits.Clear();
        foreach (var u in armies.Item1)
        {
            armyUnits.Add(u);
        }

        var newArmyMean = Vector3.zero;
        foreach (var u in armies.Item2)
        {
            newArmyMean += new Vector3(u.identityInfo.r, u.identityInfo.g, u.identityInfo.b);
        }

        newArmyMean /= armies.Item2.Count;

        var spawnCenter = transform.position;
        
        //float random = Random.Range(0f, 260f);
        //spawnCenter +=  new Vector3(Mathf.Cos(random), 0, Mathf.Sin(random)) * armyVisuals.transform.localScale.x;

        SpawnArmy(
            new IdentityInfo(newArmyMean.x, newArmyMean.y, newArmyMean.z), armies.Item2.Count, spawnCenter);

    }
    
    public GameObject SpawnArmy(IdentityInfo identity, int count, Vector3 spawnPos)
    {
        return ((MyNetworkManager)NetworkManager.singleton).SpawnArmy(identity, count, spawnPos);
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
        
        // cheat a little bit. if one of them is zero the split is trivial
        // return split 2 in place of split 1 since split1 is used for original army
        return retSplit1.Count == 0 ? (retSplit2, retSplit1) : (retSplit1, retSplit2);
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

    public void ShowUnableToBuildIcon()
    {
        if (!unableToBuildIcon.activeInHierarchy)
        {
            unableToBuildIcon.SetActive(true);
            StartCoroutine(HideUnableToBuildIconAfterDelay(iconDisplayDuration));
        }
    }

    public void SetBuildingIcon(bool active)
    {
        if (buildingIcon != null)
        {
            buildingIcon.SetActive(active);
        }
    }

    private IEnumerator HideUnableToBuildIconAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        unableToBuildIcon.SetActive(false);
    }
      #endregion

    public enum ArmyState
    {
        Idle,
        Moving,
        Attacking,
        Converting
    }
}
