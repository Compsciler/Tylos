using UnityEngine;
using Mirror;
using UnityEditor;
using System.Collections;

/// <summary>
/// 
/// </summary>
public class ArmyConversion : NetworkBehaviour
{
    [SerializeField]
    [Tooltip("Resistance to being converted by an enemy army. Scales with army size")]
    AnimationCurve resistanceCurve = new AnimationCurve();

    [SerializeField]
    [Tooltip("Resistance stops scaling at this army size")]
    private float maxUnitCount = 30f;


    // When this reaches 1, the army is converted
    [SyncVar]
    private float conversionProgress = 0f;
    public float ConversionProgress => conversionProgress;

    // Number of seconds it takes to convert the army
    [SyncVar]
    private float resistance = 0f;
    public float Resistance => resistance;

    [SerializeField]
    [Tooltip("Time it takes for the conversion progress to reset after being interrupted")]
    private float resetTime = 0.5f;

    Coroutine conversionCoroutine = null;
    private float resetTimer = 0f;

    /// <summary>
    /// Sets the resistance to being converted based on the army size
    /// Currently called by the Army class when units are added or removed
    /// </summary>
    /// <param name="unitCount"></param>
    [Server]
    public void SetResistance(int unitCount)
    {
        resistance = resistanceCurve.Evaluate(unitCount / maxUnitCount);
    }

    /// <summary>
    /// Converts the army to the other army
    /// </summary>
    /// <param name="otherArmy"></param>
    [Server]
    public void Convert(Army otherArmy)
    {
        if (conversionCoroutine == null)
        {
            StartCoroutine(ConversionCoroutine());
        }

        if (Mathf.Approximately(conversionProgress, 1f))
        {
            // Convert all units
            otherArmy.ArmyUnits.AddRange(GetComponent<Army>().ArmyUnits);
            StopCoroutine(conversionCoroutine);
            conversionCoroutine = null;
            conversionProgress = 0f;
            resetTimer = 0f;
            Destroy(gameObject);
        }
        else
        {
            conversionProgress += Time.deltaTime / resistance;
        }
    }

    /// <summary>
    /// Resets the conversion progress after a certain amount of time
    /// </summary>
    /// <returns></returns>
    IEnumerator ConversionCoroutine()
    {
        resetTimer = resetTime;
        while (resetTimer > 0f)
        {
            resetTimer -= Time.deltaTime;
            yield return null;
        }
        conversionProgress = 0f;
    }
}