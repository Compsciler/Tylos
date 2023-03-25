using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArmySelectionHandler : MonoBehaviour
{
    [SerializeField] RectTransform armySelectionArea;
    [SerializeField] LayerMask layerMask;
    [SerializeField] bool cameraAutoFollow = false;

    MyPlayer player;
    Camera mainCamera;
    CameraController cameraController;

    Vector2 startPosition;
    List<Army> selectedUnits = new List<Army>();
    public List<Army> SelectedUnits => selectedUnits;

    void Awake()
    {
        mainCamera = Camera.main;
        cameraController = mainCamera.GetComponent<CameraController>();
    }

    void Update()
    {
        if (player == null && NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            player = NetworkClient.connection.identity.GetComponent<MyPlayer>();
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            StartSelectionArea();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ClearSelectionArea();
        }
        else if (Mouse.current.leftButton.isPressed)
        {
            UpdateSelectionArea();
        }
    }

    private void StartSelectionArea()
    {
        if (!Keyboard.current.shiftKey.isPressed)
        {
            foreach (Army selectedUnit in SelectedUnits)
            {
                selectedUnit.Deselect();
            }

            SelectedUnits.Clear();
        }

        armySelectionArea.gameObject.SetActive(true);

        startPosition = Mouse.current.position.ReadValue();

        UpdateSelectionArea();
    }

    private void UpdateSelectionArea()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float areaWidth = mousePosition.x - startPosition.x;
        float areaHeight = mousePosition.y - startPosition.y;

        armySelectionArea.sizeDelta = new Vector2(Mathf.Abs(areaWidth), Mathf.Abs(areaHeight));
        armySelectionArea.anchoredPosition = startPosition +
            new Vector2(areaWidth / 2, areaHeight / 2);
    }

    private void ClearSelectionArea()
    {
        armySelectionArea.gameObject.SetActive(false);
        if (armySelectionArea.sizeDelta.magnitude == 0)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; }

            if (!hit.collider.TryGetComponent<Army>(out Army unit)) { return; }

            if (!unit.isOwned) { return; }  // Change for dev mode

            SelectedUnits.Add(unit);

            foreach (Army selectedUnit in SelectedUnits)
            {
                selectedUnit.Select();
            }

            return;
        }

        Vector2 min = armySelectionArea.anchoredPosition - (armySelectionArea.sizeDelta / 2);
        Vector2 max = armySelectionArea.anchoredPosition + (armySelectionArea.sizeDelta / 2);

        foreach (Army unit in player.MyUnits)  // Change for dev mode
        {
            if (SelectedUnits.Contains(unit)) { continue; }

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPosition.x > min.x &&
                screenPosition.x < max.x &&
                screenPosition.y > min.y &&
                screenPosition.y < max.y)
            {
                SelectedUnits.Add(unit);
                unit.Select();
            }
        }

        if (cameraAutoFollow)
        {
            cameraController.follow(selectedUnits);
        }
    }
}
