using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TeamColorRendererSetter : ColorRendererSetter
{
    #region Server

    [Server]
    public override Color GetColorToSet()
    {
        MyPlayer player = connectionToClient.identity.GetComponent<MyPlayer>();

        return player.TeamColor;
    }

    #endregion
}
