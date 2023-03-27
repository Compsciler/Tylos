using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionHandler : MonoBehaviour
{
    [SerializeField] RectTransform selectionArea;
    [SerializeField] LayerMask layerMask;
    [SerializeField] bool cameraAutoFollow = false;

    MyPlayer player;
    Camera mainCamera;
    CameraController cameraController;

    Vector2 startPosition;
    List<Army> selectedArmies = new List<Army>();
    List<Base> selectedBases = new List<Base>();
    public List<Army> SelectedArmies => selectedArmies;
    public List<Base> SelectedBases => selectedBases;

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
            foreach (Army selectedUnit in SelectedArmies)
            {
                selectedUnit.Deselect();
            }

            SelectedArmies.Clear();
        }

        selectionArea.gameObject.SetActive(true);

        startPosition = Mouse.current.position.ReadValue();

        UpdateSelectionArea();
    }

    private void UpdateSelectionArea()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float areaWidth = mousePosition.x - startPosition.x;
        float areaHeight = mousePosition.y - startPosition.y;

        selectionArea.sizeDelta = new Vector2(Mathf.Abs(areaWidth), Mathf.Abs(areaHeight));
        selectionArea.anchoredPosition = startPosition +
            new Vector2(areaWidth / 2, areaHeight / 2);
    }

    private void ClearSelectionArea()
    {
        selectionArea.gameObject.SetActive(false);
        if (selectionArea.sizeDelta.magnitude == 0) // Called when clicking on a unit
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; }

            if (hit.collider.TryGetComponent<Army>(out Army army)) {  // Army is clicked
                if (!army.isOwned) { return; }  // Change for dev mode

                SelectedArmies.Add(army);

                foreach (Army selectedArmy in SelectedArmies)
                {
                    selectedArmy.Select();
                }
            }
            // } else if(hit.collider.TryGetComponent<Base>(out Base base_)) { // Base is clicked
            //     if (!base_.isOwned) { return; }  // Change for dev mode

            //     SelectedBases.Add(base_);

            //     foreach (Base selectedBase in SelectedBases)
            //     {
            //         selectedBase.Select();
            //     }
            // }
            return;
        }

        Vector2 min = selectionArea.anchoredPosition - (selectionArea.sizeDelta / 2);
        Vector2 max = selectionArea.anchoredPosition + (selectionArea.sizeDelta / 2);

        foreach (Army unit in player.MyArmies)  // Change for dev mode
        {
            if (SelectedArmies.Contains(unit)) { continue; }

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPosition.x > min.x &&
                screenPosition.x < max.x &&
                screenPosition.y > min.y &&
                screenPosition.y < max.y)
            {
                SelectedArmies.Add(unit);
                unit.Select();
            }
        }

        if (cameraAutoFollow)
        {
            cameraController.follow(selectedArmies);
        }
    }
}
