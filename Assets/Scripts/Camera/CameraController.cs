using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class CameraController : NetworkBehaviour
{
    private List<Entity> following;
    private Controls controls;

    private Vector2 lastInput = new Vector2(0, 0);
    [SerializeField] Transform playerCameraTransform;
    [SerializeField] float max_track_speed = 1;
    [SerializeField] float keyboard_scroll_speed = 0.1f;
    [SerializeField] float edge_drag_speed = 1;
    [SerializeField] Vector2 board_min_extent = new Vector2(-5, -5);
    [SerializeField] Vector2 board_max_extent = new Vector2(5, 5);

    public override void OnStartAuthority()
    {
        playerCameraTransform.gameObject.SetActive(true);
        controls = new Controls();
        controls.Player.MoveCamera.performed += setInput;
        controls.Player.MoveCamera.canceled += setInput;
        controls.Enable();
    }

    private void setInput(InputAction.CallbackContext ctx)
    {
        lastInput = ctx.ReadValue<Vector2>();
    }

    public void follow(List<Entity> to_follow)
    {
        following = to_follow;
    }

    public void set_center(Vector3 center)
    {
        playerCameraTransform.position = new Vector3(center.x, 20, center.z);
    }

    Vector2 rel_mouse_pos()
    {
        Vector3 mouse = Input.mousePosition;
        Vector2 rel = new Vector2(mouse.x / Screen.width, mouse.y / Screen.height);
        return rel * 2 - new Vector2(1, 1);
    }

    Vector2 camera_delta(Vector2 relative_mouse_position)
    {
        Vector2 delta = new Vector2(0, 0);
        float threashold = 0.7f;
        if (relative_mouse_position.x > threashold)
        {
            delta.x = relative_mouse_position.x - threashold;
        }
        if (relative_mouse_position.x < -threashold)
        {
            delta.x = relative_mouse_position.x + threashold;
        }
        if (relative_mouse_position.y > threashold)
        {
            delta.y = relative_mouse_position.y - threashold;
        }
        if (relative_mouse_position.y < -threashold)
        {
            delta.y = relative_mouse_position.y + threashold;
        }
        return delta / (1 - threashold) * edge_drag_speed;
    }

    Vector3 avg_position(List<Entity> units)
    {
        Vector3 avg = new Vector3();
        foreach (Army u in units)
        {
            avg += u.transform.position;
        }
        return avg / units.Count;
    }

    Vector2 clamp_max_speed(Vector2 camera_translation)
    {
        if (camera_translation.magnitude > max_track_speed)
        {
            return camera_translation / camera_translation.magnitude * max_track_speed;
        }
        else
        {
            return camera_translation;
        }
    }

    void constrain_to_board()
    {
        Vector3 pos = playerCameraTransform.position;
        pos.x = Mathf.Clamp(pos.x, board_min_extent.x, board_max_extent.x);
        pos.y = 20;
        pos.z = Mathf.Clamp(pos.z, board_min_extent.y, board_max_extent.y);
        playerCameraTransform.SetPositionAndRotation(pos, playerCameraTransform.rotation);
    }


    bool centered = false;
    // Update is called once per frame
    [ClientCallback]
    void Update()
    {
        if (!centered)
        {
            // if (GetComponent<MyPlayer>().spawnLocation != null)
            // {
            // centered = true;
            // }
        }
        if (!isOwned || !Application.isFocused) { return; }

        if (following != null && following.Count > 0)
        {
            Vector3 avg = avg_position(following);
            Vector3 diff = playerCameraTransform.position - avg;
            Vector2 delta = clamp_max_speed(new Vector2(diff.x, diff.z));
            playerCameraTransform.Translate(delta);
        }
        else
        {
            Vector2 delta = camera_delta(rel_mouse_pos()) / 10f;
            playerCameraTransform.Translate(new Vector3(delta.x, delta.y));
            Vector2 keyboard_delta = lastInput * keyboard_scroll_speed;
            playerCameraTransform.Translate(new Vector3(keyboard_delta.x, keyboard_delta.y));
        }
        constrain_to_board();
        if (Camera.main)
        {
            Camera.main.transform.SetPositionAndRotation(playerCameraTransform.position, playerCameraTransform.rotation);
        }
    }
}
