using UnityEngine;
using Mirror;

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

    // Resistance to being converted by an enemy army
    // Scales with army size
    [SyncVar]
    private float resistance = 0f;
    public float Resistance => resistance;



    [Server]
    public void SetResistance(int unitCount)
    {
        resistance = resistanceCurve.Evaluate(unitCount / maxUnitCount);
    }
}
