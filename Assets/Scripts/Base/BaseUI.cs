using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BaseUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField]
    private GameObject _unitCountUI; // This game object will be attatched to canvas
    private TMP_Text _unitCountText; //Reference to the text component of _baseUIGameObject
    private Transform _canvas; 

    [Header("UI Settings")]
    [SerializeField]
    private Vector3 _offset = new Vector3(0, 90, 0);

    private void Awake() {
        if(_unitCountUI == null) {
            Debug.LogError("BaseUI: BaseUI GameObject is null");
        }

        _unitCountText = _unitCountUI.GetComponent<TMP_Text>();
    }

    void Start()
    {
        _canvas = GameObject.FindObjectOfType<Canvas>().transform;
        if(_canvas == null) {
            Debug.LogError("BaseUI: Could not find canvas in game scene");
        }
        _unitCountUI.transform.SetParent(_canvas);
    }

    void Update()
    {
        _unitCountUI.transform.position = Camera.main.WorldToScreenPoint(transform.position) + _offset; // World to screen point is used because the canvas is in screen space
    }
}
