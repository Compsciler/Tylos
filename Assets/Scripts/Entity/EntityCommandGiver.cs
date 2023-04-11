using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SelectionHandler))]
public class EntityCommandGiver : NetworkBehaviour
{
    [SerializeField] private LayerMask layerMask = new LayerMask();

    private Camera mainCamera;


    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) { return; }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; } // No hit

        if (hit.collider.TryGetComponent(out Entity entity) && !entity.isOwned)   // hit an enemy entity
        {
            TryAttack(entity);
            return;
        }
        TryMove(hit.point);
    }
    
    private void TryMove(Vector3 point)
    {
        foreach (Entity entity in SelectionHandler.SelectedEntitiesCopy)
        { 
            entity.TryMove(point);
        }
    }

    private void TryAttack(Entity target)
    {
        foreach (Entity entity in SelectionHandler.SelectedEntitiesCopy)
        {
            entity.TryAttack(target);
        }
    }

    private void TryConvert(Entity target)
    {
        foreach (Entity entity in SelectionHandler.SelectedEntitiesCopy)
        {
            entity.TryConvert(target);
        }
    }
}
