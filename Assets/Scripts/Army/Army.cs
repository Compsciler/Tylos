using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

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
    // this is our local army units copy
    // the one above will be flushed every single frame

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

    // MonoBehaviour dependencies
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

    // the mean of the army identity on complex plane
    // this is calculated on on unit add/remove
    [SyncVar] private Vector3 _meanColor;
    public Vector3 MeanColor
    {
        get => _meanColor;
        set
        {
            _meanColor = value;
            _armyIdentity.SetIdentity(value.x, value.y, value.z);
        }
    }
    private Vector2 _meanZ;
    private float _deviance;
    public float Deviance => _deviance;

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
                    if (attackTarget != null && IsInRange(attackTarget.gameObject, attackRange))
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
                    if (convertTarget != null && IsInRange(convertTarget.gameObject, attackRange))
                        // TODO: Draw convert ray
                        armyVisuals.DrawDeathRay(convertTarget.transform.position);
                    break;
                default:
                    armyAudio.StopAudio();
                    break;
            }
        }
    }

    [Server]
    private void FixedUpdate()
    {
        _armyComplex = ArmyUtils.GetArmyComplex(armyUnits);
        _meanZ = ArmyUtils.CalculateMeanZ(_armyComplex);
        ProcessMeanIdentityShift(MeanColor, ConversionRateIdle * Time.fixedDeltaTime);
    }
    #endregion

    #region Unit Operations
    public void AddUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }

    public void Absorb(List<Unit> units)
    {
        ArmyUnits.AddRange(units);
        // recalculate mean
        // there is no point doing this in the setter anymore
        // because all the list flushing already threw efficiency out of the window
        MeanColor = ArmyUtils.CalculateMeanColor(armyUnits);

        ProcessMeanIdentityShift(MeanColor, ConversionRateAbsorb);
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

        MeanColor = ArmyUtils.CalculateMeanColor(armyUnits);
        _deviance = ArmyUtils.CalculateDeviance(armyUnits, _meanColor);
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
    public List<Unit> GetArmyUnits()
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
            if (IsInRange(attackTarget.gameObject, attackRange))
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
            if (IsInRange(convertTarget.gameObject, attackRange))
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

    IEnumerator ArmyUnitsUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);

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

    // private void OnArmyUnitsUpdated(List<Unit>.Operation op, int index, Unit oldUnit, Unit newUnit)
    // {
    //     if (isServer)
    //     {
    //         if (armyVisuals == null)
    //         {
    //             return;
    //         }
    //         else
    //         {
    //             if (armyVisuals.Count != armyUnits.Count)
    //             { // If the army visuals count doesn't match the army units count, update the count and visuals
    //                 armyVisuals.SetScale(armyUnits.Count);
    //                 attackDamage = ArmyUtils.CalculateAttackPower(ArmyUnits, minUnitAttackDamage, maxUnitAttackDamage);
    //             }
    //         }
    //         armyConversion.SetResistance(ArmyUnits.Count);
    //     }
    // }

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

    private bool IsInRange(GameObject target, float range)
    {
        Vector3 offset = target.transform.position - transform.position;
        float distance = offset.magnitude;
        float trueAttackRange = attackRange + transform.lossyScale.x + target.transform.lossyScale.x; // Takes into account the size of the army and the target

        return (distance <= trueAttackRange);
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
