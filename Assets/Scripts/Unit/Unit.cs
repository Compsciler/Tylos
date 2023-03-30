using System.Collections.Generic;
using Mirror;
using UnityEngine;

[System.Serializable]
public readonly struct Unit // Make sure this struct is serializable, or else it won't work with SyncLists
{
    // fields are all readonly, to make sure they can't be changed. Changing struct fields does not trigger SyncList callbacks, so the game state will be out of sync
    // Create a new struct if you want to change a field
    public readonly IdentityInfo identityInfo;
    public readonly float health;

    // TODO: Add unit specific data her

    public Unit(IdentityInfo identityInfo, float health = 1f)
    {
        this.identityInfo = identityInfo;
        this.health = health;
    }
}
