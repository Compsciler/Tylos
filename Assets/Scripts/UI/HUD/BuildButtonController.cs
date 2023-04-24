using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildButtonController : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Button buildButton;
    private MyPlayer playerController;

    void Awake()
    {
        playerController = FindObjectOfType<MyPlayer>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        playerController.makeBase(new InputAction.CallbackContext());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        playerController.makeBase(new InputAction.CallbackContext());
    }
}
