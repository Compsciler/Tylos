using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamColorSelector : MonoBehaviour
{
    [SerializeField] Transform colorButtonsParent;
    List<Color> allTeamColorOptions = new List<Color>();
    List<Color> availableTeamColors;

    Color selectedTeamColor;
    bool hasSelectedTeamColor = false;

    void Awake()
    {
        TeamColorButton.OnTeamColorSelected += HandleTeamColorSelected;
        
        foreach (Transform colorButton in colorButtonsParent)
        {
            allTeamColorOptions.Add(colorButton.GetComponent<Image>().color);
        }
        availableTeamColors = new List<Color>(allTeamColorOptions);
    }

    void OnDestroy()
    {
        TeamColorButton.OnTeamColorSelected -= HandleTeamColorSelected;
    }

    private void HandleTeamColorSelected(Color teamColor)
    {
        if (hasSelectedTeamColor)
        {
            availableTeamColors.Add(selectedTeamColor);
        }
        selectedTeamColor = teamColor;
        availableTeamColors.Remove(teamColor);
        hasSelectedTeamColor = true;
    }
}
