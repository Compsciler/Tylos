using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public abstract class ColorRendererSetter : NetworkBehaviour
{
    [SerializeField] List<Renderer> colorRenderers;

    // Server needs to grab color from player and tell all clients to render GameObject with that color
    // Prefer SyncVar over RPC call because the RPC will happen when it is called and later joining clients (such as spectators) will not know of the color
    // Syncs at time of update or when late joining
    [SyncVar(hook = nameof(HandleColorUpdated))]
    protected Color color;

    #region Server

    public override void OnStartServer()
    {
        color = GetColorToSet();
    }

    // In case the variable that determines the color is updated before this derived OnStartServer is called, which the appropriate event will not be handled
    // [Server]
    protected abstract Color GetColorToSet();

    #endregion

    #region Client

    private void HandleColorUpdated(Color oldColor, Color newColor)
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
