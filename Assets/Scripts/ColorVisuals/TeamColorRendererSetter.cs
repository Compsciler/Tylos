using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TeamColorRendererSetter : ColorRendererSetter
{
    #region Server

    public override void OnStartServer()
    {
        base.OnStartServer();
        MyNetworkManager.ServerOnPlayerIdentityUpdated += ServerHandlePlayerIdentityUpdated;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        MyNetworkManager.ServerOnPlayerIdentityUpdated -= ServerHandlePlayerIdentityUpdated;
    }

    [Server]
    private void ServerHandlePlayerIdentityUpdated(ObjectIdentity identity)
    {
        if (identity.connectionToClient != connectionToClient) { return; }

        color = identity.GetColorFromIdentity();
    }

    [Server]
    protected override Color GetColorToSet()
    {
        if (connectionToClient == null)
        {
            return Color.gray;
        }
        MyPlayer player = connectionToClient.identity.GetComponent<MyPlayer>();

        return player.TeamColor;
    }

    #endregion
}
