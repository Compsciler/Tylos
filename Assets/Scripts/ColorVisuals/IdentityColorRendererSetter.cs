using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(ObjectIdentity))]
public class IdentityColorRendererSetter : ColorRendererSetter
{
    #region Server

    [Server]
    public override Color GetColorToSet()
    {
        ObjectIdentity identity = GetComponent<ObjectIdentity>();

        return identity.GetColorFromIdentity();
    }

    #endregion
}
