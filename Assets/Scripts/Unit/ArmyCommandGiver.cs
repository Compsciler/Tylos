using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SelectionHandler))]
public class ArmyCommandGiver : MonoBehaviour
{
    private SelectionHandler armySelectionHandler = null;
    [SerializeField] private LayerMask layerMask = new LayerMask();

    private Camera mainCamera;

    private void Awake() {
        armySelectionHandler = GetComponent<SelectionHandler>();
    }

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!Mouse.current.rightButton.wasPressedThisFrame) { return; }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; }

        TryMove(hit.point);
    }

    private void TryMove(Vector3 point)
    {
        foreach (Army unit in armySelectionHandler.SelectedArmies)
        {
            unit.UnitMovement_.CmdMove(point);
        }
    }
}
