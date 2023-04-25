using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionHandler : NetworkBehaviour
{
    [SerializeField] RectTransform selectionArea;
    [SerializeField] LayerMask layerMask;
    [SerializeField] bool cameraAutoFollow = false;

    MyPlayer player;
    Camera mainCamera;
    CameraController cameraController;

    Vector2 startPosition;
    static List<Entity> selectedEntities = new List<Entity>();
    public static List<Entity> SelectedEntities => selectedEntities;
    public static List<Entity> SelectedEntitiesCopy => new List<Entity>(selectedEntities);

    void Awake()
    {
        mainCamera = Camera.main;
        cameraController = mainCamera.GetComponent<CameraController>();

        player = NetworkClient.connection.identity.GetComponent<MyPlayer>();

        GameOverHandler.ClientOnGameOver += ClientHandleGameOver;
    }

    void OnDestroy()
    {
        GameOverHandler.ClientOnGameOver -= ClientHandleGameOver;
    }

    void Update()
    {
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

    [Client]
    public static void AddToSelection(Entity entity)
    {
        if (entity == null) { return; }  // Hotfix for null reference exception
        if (SelectedEntities.Contains(entity)) { return; }

        selectedEntities.Add(entity);
        entity.Select();
    }

    private void StartSelectionArea()
    {
        if (!Keyboard.current.shiftKey.isPressed)
        {
            foreach (Entity selectedEntity in SelectedEntities)
            {
                selectedEntity.Deselect();
            }

            SelectedEntities.Clear();
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
        if (selectionArea.sizeDelta.magnitude == 0) // User clicked instead of dragging a box
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; }

            if (hit.collider.TryGetComponent<Entity>(out Entity entity))
            {// Entity is clicked
                if (!entity.isOwned) { return; }  // Change for dev mode

                SelectedEntities.Add(entity);

                foreach (Entity selectedEntity in SelectedEntities)
                {
                    selectedEntity.Select();
                }
            }
            return;
        }

        Vector2 min = selectionArea.anchoredPosition - (selectionArea.sizeDelta / 2);
        Vector2 max = selectionArea.anchoredPosition + (selectionArea.sizeDelta / 2);

        // Selects all the armies in the selection area if it is player's armies
        foreach (Army unit in player.MyArmies)  // Change for dev mode
        {
            if (SelectedEntities.Contains((Entity)unit)) { continue; }

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(unit.transform.position);

            if (screenPosition.x > min.x &&
                screenPosition.x < max.x &&
                screenPosition.y > min.y &&
                screenPosition.y < max.y)
            {
                SelectedEntities.Add(unit);
                unit.Select();
            }
        }

        // Selects all the bases in the selection area if it is player's base
        foreach (Base myBase in player.MyBases)  // Change for dev mode
        {
            if (SelectedEntities.Contains((Entity)myBase)) { continue; }

            Vector3 screenPosition = mainCamera.WorldToScreenPoint(myBase.transform.position);

            if (screenPosition.x > min.x &&
                screenPosition.x < max.x &&
                screenPosition.y > min.y &&
                screenPosition.y < max.y)
            {
                SelectedEntities.Add(myBase);
                myBase.Select();
            }
        }

        if (cameraAutoFollow)
        {
            cameraController.follow(selectedEntities);
        }
    }

    [Client]
    private void ClientHandleGameOver(string winnerName)
    {
        enabled = false;
    }
}
