using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections.ObjectModel;
using System.Linq;

public class ArmyUtils : MonoBehaviour
{
    const float MIN_COLOR_VALUE = 0f;
    const float MAX_COLOR_VALUE = 255f;

    public static float CalculateAttackPower(List<Unit> units, float minUnitAttackDamage, float maxUnitAttackDamage)
    {
        float attackPower = 0f;
        foreach (Unit unit in units)
        {
            float r = unit.identityInfo.r;
            attackPower += Map(r, MIN_COLOR_VALUE, MAX_COLOR_VALUE, minUnitAttackDamage, maxUnitAttackDamage);
        }
        return attackPower;
    }

    public static float CalculateHealth(List<Unit> units)
    {
        float health = 0f;
        foreach (Unit unit in units)
        {
            float g = unit.identityInfo.g;
            health += Map(g, 0f, 255f, 1f, 2f);
        }
        Debug.Log("Army health: " + health);
        return health;
    }

    public static Vector3 CalculateMeanColor(List<Unit> armyUnits)
    {
        // Calculate and set the new mean color
        Vector3 meanColor = Vector3.zero;
        meanColor = Vector3.zero;
        foreach (var u in armyUnits)
        {
            meanColor += new Vector3(u.identityInfo.r, u.identityInfo.g, u.identityInfo.b);
        }
        meanColor /= armyUnits.Count;
        return meanColor;
    }

    public static float CalculateDeviance(List<Unit> armyUnits, Vector3 meanColor)
    {
        var armyIdentityColors = armyUnits.Select
            (i => new Vector3(i.identityInfo.r, i.identityInfo.g, i.identityInfo.b)).ToList();
        if (armyIdentityColors.Count == 0)
        {
            return 0f;
        }
        var mean = meanColor;
        // using squared magnitude because it's like a standard
        // PCA uses another three stdev metric
        // is there a way to unify them to reduce problems?
        var stdev = armyIdentityColors.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyIdentityColors.Count;
        return stdev;
    }

    public static Vector2 CalculateMeanZ(List<Vector2> armyComplex)
    {
        Vector2 meanZ = Vector2.zero;
        foreach (var z in armyComplex)
        {
            meanZ += z;
        }
        meanZ /= armyComplex.Count;
        return meanZ;
    }

    public static List<Vector2> GetArmyComplex(List<Unit> armyUnits)
    {
        List<Vector2> armyComplex;
        armyComplex = armyUnits.Select(i => i.GetIdentityZ()).ToList();
        return armyComplex;
    }

    // This is a function that maps a value from one range to another
    public static float Map(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
    }
}
