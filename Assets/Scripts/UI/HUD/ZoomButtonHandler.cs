using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZoomButtonHandler : MonoBehaviour
{
    private CameraController cameraController;

    private void Start()
    {
        cameraController = FindObjectOfType<CameraController>();
    }

    public void OnZoomInButtonClicked()
    {
        cameraController.ZoomIn();
    }

    public void OnZoomOutButtonClicked()
    {
        cameraController.ZoomOut();
    }
}
