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

    private Controls _controls;

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
    }

    public void OnMouseEnterUI()
    {
        SetCursor(defaultPointer);
    }

    public void OnMouseExitUI()
    {
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

    public void OnMoveCamera(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            SetCursor(FindCursorType("CameraMove"));
        }
        else if (context.canceled)
        {
            SetCursor(defaultCursor);
        }
    }

    public void OnMakeBase(InputAction.CallbackContext context)
    {
        throw new System.NotImplementedException();
    }
}