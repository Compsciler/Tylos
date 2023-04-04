using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections.ObjectModel;
public class ArmyUtils : MonoBehaviour
{
    const float MIN_COLOR_VALUE = 0f;
    const float MAX_COLOR_VALUE = 255f;
    const float MIN_UNIT_ATTACK_POWER = 1f;
    const float MAX_UNIT_ATTACK_POWER = 2f;
    public static float CalculateAttackPower(SyncList<Unit> units)
    {
        float attackPower = 0f;
        foreach (Unit unit in units)
        {
            float r = unit.identityInfo.r;
            attackPower += Map(r, MIN_COLOR_VALUE, MAX_COLOR_VALUE, MIN_UNIT_ATTACK_POWER, MAX_UNIT_ATTACK_POWER);
        }
        return attackPower;
    }

    public static float CalculateHealth(SyncList<Unit> units)
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

    // This is a function that maps a value from one range to another
    public static float Map(float value, float oldMin, float oldMax, float newMin, float newMax)
    {
        return (value - oldMin) / (oldMax - oldMin) * (newMax - newMin) + newMin;
    }
}
