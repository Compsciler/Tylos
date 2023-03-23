using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TeamColorButton : MonoBehaviour
{
    Color teamColor;
    public Color TeamColor => teamColor;

    public static event Action<TeamColorButton> OnColorButtonSelected;  // Use TeamColorSelector reference instead? TeamColorButton relies on TeamColorSelector existing

    [SerializeField] UnityEvent onColorPlayerSelected;
    [SerializeField] UnityEvent onColorMadeAvailable;
    [SerializeField] UnityEvent onColorMadeUnavailable;

    void Awake()
    {
        teamColor = GetComponent<Image>().color;  // Assumes Color image for now
    }

    public void SelectColorButton()  // Or TrySelectColorButton?
    {
        OnColorButtonSelected?.Invoke(this);
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
