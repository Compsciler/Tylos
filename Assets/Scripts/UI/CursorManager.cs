using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorManager : MonoBehaviour
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

    private bool isCursorOverUI;

    void Start()
    {
        SetCursor(defaultCursor);
    }

    void Update()
    {
        UpdateCursor();
    }

    public void OnMouseEnterUI()
    {
        isCursorOverUI = true;
        SetCursor(defaultPointer);
    }

    public void OnMouseExitUI()
    {
        isCursorOverUI = false;
        UpdateCursor();
    }

    void UpdateCursor()
    {
        //Do not update if cursor is over a UI element
        if (isCursorOverUI) return;
        
        /*
        
        //Perform raycast
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out hit)) return;
        
        //Store reference to the hit object
        GameObject hitObject = hit.collider.gameObject;

        // Customize these conditions based on the hit object's tags and the user's input
        if (hitObject.CompareTag("Enemy") && Input.GetKey(KeyCode.A))
        {
            SetCursor(FindCursorType("attack"));
        }
        else if (hitObject.CompareTag("Resource") && Input.GetKey(KeyCode.G))
        {
            SetCursor(FindCursorType("gather"));
        }
        
        */
            
        //If no conditions are met, set to default cursor
        else
        {
            SetCursor(defaultCursor);
        }
    }

    CursorType FindCursorType(string nameIn)
    {
        return cursorTypes.Find(cursor => cursor.name == nameIn);
    }

    void SetCursor(CursorType cursorType)
    {
        if (cursorType != null)
        {
            Cursor.SetCursor(cursorType.texture, cursorType.hotspot, CursorMode.Auto);
        }
    }
}