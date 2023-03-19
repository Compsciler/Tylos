using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TeamColorRendererSetter : ColorRendererSetter
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        MyNetworkManager.ServerOnPlayerIdentityUpdated += HandlePlayerIdentityUpdated;
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        MyNetworkManager.ServerOnPlayerIdentityUpdated -= HandlePlayerIdentityUpdated;
    }

    private void HandlePlayerIdentityUpdated(ObjectIdentity identity)
    {
        if (identity.connectionToClient != connectionToClient) { return; }
        
        color = identity.GetColorFromIdentity();
    }
    
    [Server]
    public override Color GetColorToSet()
    {
        MyPlayer player = connectionToClient.identity.GetComponent<MyPlayer>();

        return player.TeamColor;
    }
}
