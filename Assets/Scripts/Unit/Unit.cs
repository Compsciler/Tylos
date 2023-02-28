using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public class Unit : NetworkBehaviour
{
    [SerializeField] UnityEvent onSelected;
    [SerializeField] UnityEvent onDeselected;

    #region Client

    [Client]
    public void Select()
    {
        if (!isOwned) { return; }

        onSelected?.Invoke();
    }

    [Client]
    public void Deselect()
    {
        if (!isOwned) { return; }

        onDeselected?.Invoke();
    }

    #endregion
}
