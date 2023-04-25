using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorManager : MonoBehaviour, Controls.IPlayerActions
{
    [System.Serializable]
    public class CursorType
    {
        public string name;
        public Texture2D texture;
        public Vector2 hotspot;
    }

    public CursorType defaultCursor;
    public CursorType defaultPointer;
    public List<CursorType> cursorTypes;
    private float edgePanCursorTimer = 0f;
    private float zoomCursorTimer = 0f;
    private Controls _controls;
    private CameraController cameraController;
    private bool isMouseOverUI = false;

    void Awake()
    {
        _controls = new Controls();
        _controls.Player.SetCallbacks(this);
    }

    void OnEnable()
    {
        _controls.Enable();
    }

    void OnDisable()
    {
        _controls.Disable();
    }
    void Start()
    {
        SetCursor(defaultCursor);
        StartCoroutine(WaitForCameraController());
    }

    void Update()
    {
        if (zoomCursorTimer > 0)
        {
            zoomCursorTimer -= Time.deltaTime;
            if (zoomCursorTimer <= 0)
            {
                isMouseOverUI = false;
            }
        }
        if (edgePanCursorTimer > 0)
        {
            edgePanCursorTimer -= Time.deltaTime;
            if (edgePanCursorTimer <= 0)
            {
                isMouseOverUI = false;
            }
        }

        if (!isMouseOverUI)
        {
            if (IsMouseOverInteractable())
            {
                SetInteractableCursor();
            }
            else
            {
                SetCursor(defaultCursor);
            }
        }
    }

    public void OnMouseEnterUI()
    {
        isMouseOverUI = true;
        SetCursor(defaultPointer);
    }

    public void OnMouseExitUI()
    {
        isMouseOverUI = false;
        SetCursor(defaultCursor);
    }

    CursorType FindCursorType(string nameIn)
    {
        return cursorTypes.Find(cursor => cursor.name == nameIn);
    }

    void SetCursor(CursorType cursorType)
    {
        if (cursorType == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(cursorType.texture, cursorType.hotspot, CursorMode.Auto);
        }
    }

    public void SetInteractableCursor()
    {
        SetCursor(FindCursorType("Interactable"));
    }

    public void SetDefaultCursor()
    {
        SetCursor(defaultCursor);
    }

    public void OnMoveCamera(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            isMouseOverUI = true;
            SetCursor(FindCursorType("CameraMove"));
        }
        else if (context.canceled)
        {
            isMouseOverUI = false;
            SetCursor(defaultCursor);
        }
    }

    public void OnMakeBase(InputAction.CallbackContext context)
    {
        return;
    }

    public void OnZoom(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isMouseOverUI = true;
            zoomCursorTimer = 0.2f; // Adjust this value to control how long the zoom cursor stays visible
            SetCursor(FindCursorType("CameraZoom"));
        }
    }

    private void OnEdgePanning()
    {
        isMouseOverUI = true;
        edgePanCursorTimer = 0.2f;
        SetCursor(FindCursorType("CameraMove"));
    }

    private IEnumerator WaitForCameraController()
    {
        while (cameraController == null)
        {
            cameraController = FindObjectOfType<CameraController>();
            yield return null;
        }

        // Subscribe to the EdgePanning event after the CameraController is found
        cameraController.EdgePanning += OnEdgePanning;
    }

    private bool IsMouseOverInteractable()
    {
        if (Camera.main == null) return false;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        return (Physics.Raycast(ray, out hit) && IsOwnedInteractable(hit.collider.gameObject));
    }
    private bool IsOwnedInteractable(GameObject hitObject)
    {
        Army army = hitObject.GetComponentInParent<Army>();
        Base baseObj = hitObject.GetComponent<Base>();

        if (army != null && army.isOwned)
        {
            return true;
        }
        else if (baseObj != null && baseObj.isOwned)
        {
            return true;
        }

        return false;
    }

    public void OnCenterCamera(InputAction.CallbackContext context)
    {
        //do nothing, needed to implement interface
    }
}