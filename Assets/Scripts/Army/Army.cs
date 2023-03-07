using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

public class Army
{
    List<Unit> armyUnits = new List<Unit>();  // Change to set if necessary
    public ReadOnlyCollection<Unit> ArmyUnits => armyUnits.AsReadOnly();

    public Army() {}

    public void AddUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }
    public void RemoveUnit(Unit unit)
    {
        armyUnits.Add(unit);
    }
}
