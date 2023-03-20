using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TeamColorButton : MonoBehaviour
{
    Color teamColor;
    public Color TeamColor => teamColor;

    public static event Action<Color> OnTeamColorSelected;  // Use TeamColorSelector reference instead? TeamColorButton relies on TeamColorSelector existing

    void Awake()
    {
        teamColor = GetComponent<Image>().color;  // Assumes Color image for now
    }

    public void SelectColor()
    {
        OnTeamColorSelected?.Invoke(teamColor);
    }
}
