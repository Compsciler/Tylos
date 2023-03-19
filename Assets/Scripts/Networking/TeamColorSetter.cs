using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TeamColorSetter : NetworkBehaviour
{
    [SerializeField] List<Renderer> colorRenderers;

    // Server needs to grab color from player and tell all clients to render GameObject with that color
    // Prefer SyncVar over RPC call because the RPC will happen when it is called and later joining clients (such as spectators) will not know of the color
    // Syncs at time of update or when late joining
    [SyncVar(hook = nameof(HandleTeamColorUpdated))]
    Color teamColor;

    #region Server

    public override void OnStartServer()
    {
        MyPlayer player = connectionToClient.identity.GetComponent<MyPlayer>();

        teamColor = player.TeamColor;
    }

    #endregion

    #region Client

    private void HandleTeamColorUpdated(Color oldColor, Color newColor)
    {
        foreach (Renderer renderer in colorRenderers)
        {
            if (renderer is SpriteRenderer spriteRenderer)
            {
                spriteRenderer.color = newColor;
                continue;
            }
            renderer.material.SetColor("_BaseColor", newColor);
        }
    }

    #endregion
}
