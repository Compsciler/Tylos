using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class MyNetworkManager : NetworkManager
{
    [SerializeField] GameObject unitSpawnerPrefab;

    NetworkConnectionToClient hostConnection = null;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        // TODO: move into StartGame() later
        // If this is the first player to join, make them the host
        if (hostConnection == null)
        {
            hostConnection = conn;
        }

        MyPlayer player = conn.identity.GetComponent<MyPlayer>();
        player.SetTeamColor(TeamColorAssigner.Instance.GetAndRemoveRandomColor());

        GameObject unitSpawnerInstance = Instantiate(unitSpawnerPrefab, conn.identity.transform.position, conn.identity.transform.rotation);
        NetworkServer.Spawn(unitSpawnerInstance, conn);
    }
}
