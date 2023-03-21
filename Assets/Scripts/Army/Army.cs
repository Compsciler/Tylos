using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

public class Army
{
    List<Unit> armyUnits = new List<Unit>();  // Change to set if necessary
    public ReadOnlyCollection<Unit> ArmyUnits => armyUnits.AsReadOnly();
    public float GetDeviance() {
        // complex sum to find centroid
        var sum = Vector2.zero;
        var armyComplex = new List<Vector2>();
        foreach (var u in armyUnits)
        {
            var identity = u.GetComponent<ObjectIdentity>().Identity;
            var rgbIdentity = new Color(identity.r, identity.g, identity.b);
            float h;
            Color.RGBToHSV(rgbIdentity, out h, out _, out _);
            //0->0, 1->2pi
            var angle = 2 * Mathf.PI * h;
            var complex = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            armyComplex.Add(complex);
        }
        sum = armyComplex.Aggregate(sum, (current, c) => current + c);
        var mean = sum / armyComplex.Count;
        // using squared magnitude because it's like a standard
        var stdev = armyComplex.Sum(c => (c - mean).sqrMagnitude);
        stdev /= armyComplex.Count;
        return stdev;
    }
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
