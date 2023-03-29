using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections.ObjectModel;
public class ArmyUtils : MonoBehaviour
{
    public static float CalculateAttackPower(ReadOnlyCollection<Unit> units)  
    {
        float attackPower = 0f;
        foreach (Unit unit in units)
        {
            float r = unit.IdentityInfo.r; 
            attackPower +=  Map(r, 0f, 255f, 1f, 2f);
        }
        Debug.Log("Army attack power: " + attackPower);
        return attackPower;
    }

    public static float CalculateHealth(ReadOnlyCollection<Unit> units)
    {
        float health = 0f;
        foreach (Unit unit in units)
        {
            float g = unit.IdentityInfo.g;
            health += Map(g, 0f, 255f, 1f, 2f);
        }
        Debug.Log("Army health: " + health);
        return health;
    }

    // This is a function that maps a value from one range to another
    public static float Map(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
    }
}
