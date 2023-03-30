using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
///  This class keeps track of how much damage an army has taken
///  Army script will access this class to determine how much damage to deal to a unit, and remove units from the army when they die
/// </summary>
[RequireComponent(typeof(Army))]
public class ArmyHealth : EntityHealth
{
    Army army;
    private void Awake() {
        army = GetComponent<Army>();
        if(army == null) {
            Debug.LogError("ArmyHealth requires an Army component");
        }
    }

    #region Server

    [Server]
    public virtual void TakeDamage(float damage)
    {
        Unit unit = army.ArmyUnits[army.ArmyUnits.Count - 1];
        float health = unit.health;
        health -= damage;

        if(health <= 0) {
            army.ArmyUnits.RemoveAt(army.ArmyUnits.Count - 1);
            if(army.ArmyUnits.Count == 0) {
                Die();
            }
        } else {
            army.ArmyUnits[army.ArmyUnits.Count - 1] = new Unit(unit.identityInfo, health);
        }
    }
    #endregion
}
