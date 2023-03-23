using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerArmies : MonoBehaviour
{
    public List<Army> myArmies = new List<Army>();
    public List<Army> MyArmies => myArmies;
    public Dictionary<Army, Army> unitToArmy = new Dictionary<Army, Army>();

    public void AddUnitToNewArmy(Army unit)
    {
        Army army = new Army();
        myArmies.Add(army);
        AddUnitToArmy(unit, army);
    }
    
    public void AddUnitToArmy(Army unit, Army army)
    {
        if (unitToArmy.ContainsKey(unit)) 
        {
            if (unitToArmy[unit] == army) { return; }
            
            unitToArmy[unit].RemoveUnit(unit);
        }

        army.AddUnit(unit);
        unitToArmy[unit] = army;
    }

    public void RemoveUnitFromArmy(Army unit)
    {
        if (!unitToArmy.ContainsKey(unit)) { return; }

        Army army = unitToArmy[unit];
        army.RemoveUnit(unit);
        unitToArmy.Remove(unit);
        if (army.ArmyUnits.Count == 0)
        {
            myArmies.Remove(army);
        }
    }
}
