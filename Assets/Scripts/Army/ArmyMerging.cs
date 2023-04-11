using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Army))]
public class ArmyMerging : NetworkBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!isServer) { return; }  // Probably unnecessary, tests say this is never called on the client

        if (!other.TryGetComponent<Army>(out Army otherArmy)) { return; }

        if (otherArmy.connectionToClient.connectionId != connectionToClient.connectionId) { return; }  // Prevents merging with enemy armies (but we can implement that somewhere else later)

        if (!ShouldThisArmyMerge(otherArmy)) { return; }  // Prevents double merging

        MergeWithArmy(otherArmy);
    }

    private void MergeWithArmy(Army otherArmy)
    {
        Army army = GetComponent<Army>();

        army.ArmyUnits.AddRange(otherArmy.ArmyUnits);

        Destroy(otherArmy.gameObject);
    }

    private bool ShouldThisArmyMerge(Army otherArmy)
    {
        Army army = GetComponent<Army>();

        if (army.ArmyUnits.Count != otherArmy.ArmyUnits.Count)
        {
            return army.ArmyUnits.Count > otherArmy.ArmyUnits.Count;
        }

        return gameObject.GetInstanceID() < otherArmy.gameObject.GetInstanceID();
    }
}
