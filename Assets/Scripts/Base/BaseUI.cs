using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(Base))]
public class BaseUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField]
    private GameObject _unitCountUI; // This game object will be attatched to canvas
    [SerializeField]
    private GameObject _baseHealthUI; // This game object will be attatched to canvas
    

    [Header("UI Settings")]
    [SerializeField]
    private Vector3 _offset = new Vector3(0, 90, 0);

    // Internal References
    private TMP_Text _unitCountText; //Reference to the text component of _baseUIGameObject
    private Transform _canvas; 
    private Base _base;
    private Image _healthBarFill;

    private void Awake() {
        if(_unitCountUI == null) {
            Debug.LogError("BaseUI: BaseUI GameObject is null");
        }

        _unitCountText = _unitCountUI.GetComponent<TMP_Text>();
        _base = GetComponent<Base>();
        _healthBarFill = _baseHealthUI.GetComponent<Image>();
    }

    void Start()
    {
        _canvas = GameObject.FindObjectOfType<Canvas>().transform;
        if(_canvas == null) {
            Debug.LogError("BaseUI: Could not find canvas in game scene");
        }
        _unitCountUI.transform.SetParent(_canvas);
        _baseHealthUI.transform.SetParent(_canvas);
    }

    void Update()
    {
        _unitCountUI.transform.position = Camera.main.WorldToScreenPoint(transform.position) + 2.3f*_offset; // World to screen point is used because the canvas is in screen space
        _baseHealthUI.transform.position = Camera.main.WorldToScreenPoint(transform.position) + _offset;
        _unitCountText.text = _base.GetBaseUnitCount().ToString();
        _healthBarFill.fillAmount = _base.GetBaseHealth() / 100.0f;
    }

    void OnDisable() {
        _unitCountUI.SetActive(false);
        _baseHealthUI.SetActive(false);
    }

    void OnDestroy() {
        Destroy(_unitCountUI); // Clean up
        Destroy(_baseHealthUI);
    }
}
