using System;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TeamColorButton : MonoBehaviour
{
    Color teamColor;
    public Color TeamColor => teamColor;

    public static event Action<TeamColorButton, int> OnColorButtonSelected;  // Use TeamColorSelector reference instead? TeamColorButton relies on TeamColorSelector existing

    [SerializeField] UnityEvent onColorPlayerSelected;
    [SerializeField] UnityEvent onColorMadeAvailable;
    [SerializeField] UnityEvent onColorMadeUnavailable;

    void Awake()
    {
        teamColor = GetComponent<Image>().color;  // Assumes Color image for now
    }

    public void SelectColorButton()  // Or TrySelectColorButton?
    {
        int playerConnectionId = NetworkClient.connection.connectionId;
        OnColorButtonSelected?.Invoke(this, playerConnectionId);
    }

    public void HandleColorPlayerSelected()
    {
        onColorPlayerSelected?.Invoke();
    }
    public void HandleColorMadeAvailable()
    {
        onColorMadeAvailable?.Invoke();
    }
    public void HandleColorMadeUnavailable()
    {
        onColorMadeUnavailable?.Invoke();
    }
}
