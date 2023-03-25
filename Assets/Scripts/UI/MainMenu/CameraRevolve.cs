using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRevolve : MonoBehaviour
{
    public float rotationSpeed = 10f;

    private void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
}
