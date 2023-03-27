using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.Events;

public abstract class Entity : NetworkBehaviour
{
    public UnityEvent onSelected;
    public UnityEvent onDeselected;
    
    [Client]
    public void Select()
    {
        if (!isOwned) { return; }  // Change for dev mode, check may also be redundant from UnitSelectionHandler

        onSelected?.Invoke();
    }

    [Client]
    public void Deselect()
    {
        if (!isOwned) { return; }

        onDeselected?.Invoke();
    }
}
