using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private List<Entity> following;
    [SerializeField] float max_track_speed = 1;
    [SerializeField] float keyboard_scroll_speed = 0.1f;
    [SerializeField] float edge_drag_speed = 1;

    public void follow(List<Entity> to_follow){
        following = to_follow;
    }

    Vector2 rel_mouse_pos(){
        Vector3 mouse = Input.mousePosition;
        Vector2 rel = new Vector2(mouse.x / Screen.width, mouse.y / Screen.height);
        return rel * 2 - new Vector2(1,1);
    }

    Vector2 camera_delta(Vector2 relative_mouse_position){
        Vector2 delta = new Vector2(0,0);
        float threashold = 0.7f;
        if(relative_mouse_position.x > threashold){
            delta.x = relative_mouse_position.x - threashold;
        }
        if(relative_mouse_position.x < -threashold){
            delta.x = relative_mouse_position.x + threashold;
        }
        if(relative_mouse_position.y > threashold){
            delta.y = relative_mouse_position.y - threashold;
        }
        if(relative_mouse_position.y < -threashold){
            delta.y = relative_mouse_position.y + threashold;
        }
        return delta / (1 - threashold) * edge_drag_speed;
    }

    Vector3 avg_position(List<Entity> units){
        Vector3 avg = new Vector3();
        foreach(Army u in units){
            avg += u.transform.position;
        }
        return avg / units.Count;
    }

    Vector2 clamp_max_speed(Vector2 camera_translation){
        if(camera_translation.magnitude > max_track_speed){
            return camera_translation / camera_translation.magnitude * max_track_speed;
        } else {
            return camera_translation;
        }
    }

    Vector2 keyboard_movement(bool forward, bool backward, bool left, bool right){
        Vector2 movement = new Vector2();
        if(forward) movement.y += 1;
        if(backward) movement.y -= 1;
        if(left) movement.x -= 1;
        if(right) movement.x += 1;
        return movement * keyboard_scroll_speed;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButton(0)){
        }
        if(following != null && following.Count > 0){
            Vector3 avg = avg_position(following);
            Vector3 diff = Camera.main.transform.position - avg;
            Vector2 delta = clamp_max_speed(new Vector2(diff.x, diff.z));
            Camera.main.transform.Translate(delta);
        } else {
            Vector2 delta = camera_delta(rel_mouse_pos()) / 10f;
            Camera.main.transform.Translate(new Vector3(delta.x, delta.y));
            bool forward = Input.GetKey(KeyCode.W);
            bool backward = Input.GetKey(KeyCode.S);
            bool left = Input.GetKey(KeyCode.A);
            bool right = Input.GetKey(KeyCode.D);
            Vector2 keyboard_delta = keyboard_movement(forward, backward, left, right);
            Camera.main.transform.Translate(new Vector3(keyboard_delta.x, keyboard_delta.y));

        }
        // Input.mousePosition
    }
}
