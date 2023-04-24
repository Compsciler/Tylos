using System;
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
    public Mode Mode_
    {
        get => mode;
        set => mode = value;
    }
    private Mode prevMode;

    public static event Action<Mode> AuthorityOnModeChanged;


    void Awake()
    {
        GameOverHandler.ClientOnGameOver += ClientHandleGameOver;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        AuthorityOnModeChanged?.Invoke(mode);
    }

    void OnDestroy()
    {
        GameOverHandler.ClientOnGameOver -= ClientHandleGameOver;
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
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            mode = Mode.Attack;
            Debug.Log("Mode: " + mode.ToString());
        }
        else if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            mode = Mode.Convert;
            Debug.Log("Mode: " + mode.ToString());
        }

        if (mode != prevMode)
        {
            AuthorityOnModeChanged?.Invoke(mode);
            // MirrorUtils.PrintNetworkInfo(this);
        }
        prevMode = mode;


        if (!Mouse.current.rightButton.wasPressedThisFrame) { return; }

        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) { return; } // No hit

        if (hit.collider.TryGetComponent(out Entity entity) && !entity.isOwned)   // hit an enemy entity
        {
            if (mode == Mode.Attack)
            {
                TryAttack(entity);
            }
            else if (mode == Mode.Convert)
            {
                TryConvert(entity);
            }
        }
        else
        {
            TryMove(hit.point);
        }
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

    [Client]
    private void ClientHandleGameOver(string winnerName)
    {
        enabled = false;
    }

    public enum Mode
    {
        Attack,
        Convert
    }
}
