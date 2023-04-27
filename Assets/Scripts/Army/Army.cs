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

    private Vector2 _meanZ;
    private float deviance;
    public float Deviance => deviance;

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
    private void ServerHandleArmyUnitsUpdated()
    {
        deviance = ArmyUtils.CalculateDeviance(ArmyUnits, _armyIdentity.IdentityVec3);
        armyVisuals.SetScale(armyUnits.Count);
        attackDamage = ArmyUtils.CalculateAttackPower(ArmyUnits, minUnitAttackDamage, maxUnitAttackDamage);
        armyConversion.SetResistance(ArmyUnits.Count);
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
