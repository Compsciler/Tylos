using System.Collections.Generic;
using Mirror;
using UnityEngine;

[System.Serializable]
public struct Unit // Make sure this struct is serializable, or else it won't work with SyncLists
{
    // fields are all readonly, to make sure they can't be changed. Changing struct fields does not trigger SyncList callbacks, so the game state will be out of sync
    // Create a new struct if you want to change a field
    public IdentityInfo identityInfo;
    public float health;


    // TODO: Add unit specific data her

    public Unit(IdentityInfo identityInfo, float health = 1f)
    {
        this.identityInfo = identityInfo;
        this.health = health;
    }

    public Vector2 GetIdentityZ()
    {
        var identity = identityInfo;
        var rgbIdentity = new Color(identity.r, identity.g, identity.b);
        float h;
        Color.RGBToHSV(rgbIdentity, out h, out _, out _);
        //0->0, 1->2pi
        var angle = 2 * Mathf.PI * h;
        // construct a complex number from angle and return it
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    // this is fine right?
    public void SetIdentity(float r, float g, float b)
    {
        identityInfo.r = r;
        identityInfo.g = g;
        identityInfo.b = b;
    }
    
    public void SetIdentity(Color c)
    {
        identityInfo.r = c.r;
        identityInfo.g = c.g;
        identityInfo.b = c.b;
    }

    public Unit Clone()
    {
        return new Unit(new IdentityInfo(identityInfo.r, identityInfo.g, identityInfo.b), this.health);
    }
}
    
