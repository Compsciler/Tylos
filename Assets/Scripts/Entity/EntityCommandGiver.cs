using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SelectionHandler))]
public class EntityCommandGiver : NetworkBehaviour
{
    [SerializeField] private LayerMask layerMask = new LayerMask();

    private Camera mainCamera;
    private Mode mode = Mode.Attack; // Determines what right click does 


    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // if you press space you switch between attack and convert
        // TODO: make this use the new input system
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            mode = mode == Mode.Attack ? Mode.Convert : Mode.Attack;
            Debug.Log("Mode: " + mode.ToString());
        }


        if (!Mouse.current.rightButton.wasPressedThisFrame) { return; }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; } // No hit

        if (hit.collider.TryGetComponent(out Entity entity) && !entity.isOwned)   // hit an enemy entity
        {
            if(mode == Mode.Attack)
            {
                TryAttack(entity);
            }
            else if(mode == Mode.Convert)
            {
                TryConvert(entity);
            }
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

    enum Mode
    {
        Attack,
        Convert
    }
}
