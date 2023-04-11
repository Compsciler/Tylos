using UnityEngine;
using Mirror;
using UnityEditor;
using System.Collections;

/// <summary>
/// 
/// </summary>
public class ArmyConversion : NetworkBehaviour
{
    // Resistance stops scaling at this value
    [SerializeField]
    private float maxUnitCount = 30f;

    [SerializeField]
    AnimationCurve resistanceCurve = new AnimationCurve();

    [SyncVar]
    private float conversionProgress = 0f;
    public float ConversionProgress => conversionProgress;

    // Resistance to being converted by an enemy army
    // Scales with army size
    [SyncVar]
    private float resistance = 0f;
    public float Resistance => resistance;

    // How long it takes to reset the conversion progress
    [SerializeField]
    private float resetTime = 0.5f;

    Coroutine conversionCoroutine = null;
    private float resetTimer = 0f;

    [Server]
    public void SetResistance(int unitCount)
    {
        resistance = resistanceCurve.Evaluate(unitCount / maxUnitCount);
    }

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