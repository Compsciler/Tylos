using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

/// <summary>
/// Base class for all entities in the game.
/// Entities are objects that can be selected and deselected.
/// For example, an army or a base
/// </summary>
[RequireComponent(typeof(EntityMovement))]
public abstract class Entity : NetworkBehaviour
{
    public UnityEvent onSelected;
    public UnityEvent onDeselected;


    [SerializeField]
    protected EntityMovement entityMovement;
    public EntityMovement EntityMovement => entityMovement;

    [Client]
    public virtual void Select()
    {
        if (!isOwned) { return; }  // Change for dev mode, check may also be redundant from UnitSelectionHandler

        onSelected?.Invoke();
    }

    [Client]
    public virtual void Deselect()
    {
        if (!isOwned) { return; }

        onDeselected?.Invoke();
    }

    [Client]
    public virtual void TryMove(Vector3 position) {
        if (!isOwned) { return; }

        Debug.Log("Entity TryMove");
        entityMovement.CmdMove(position);
    }
}
