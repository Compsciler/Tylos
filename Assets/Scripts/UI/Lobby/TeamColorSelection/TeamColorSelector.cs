using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class TeamColorSelector : NetworkBehaviour
{
    [SerializeField] Transform colorButtonsParent;
    List<TeamColorButton> colorButtons = new List<TeamColorButton>();
    List<Color> allTeamColorOptions = new List<Color>();

    [SyncVar(hook = nameof(HandleAvailableTeamColorsUpdated))]
    List<Color> availableTeamColors;

    Color selectedTeamColor;
    bool hasSelectedTeamColor = false;

    #region Server

    public override void OnStartServer()
    {
        foreach (Transform colorButtonTransform in colorButtonsParent)
        {
            TeamColorButton colorButton = colorButtonTransform.GetComponent<TeamColorButton>();
            colorButtons.Add(colorButton);
            allTeamColorOptions.Add(colorButton.TeamColor);
        }
        availableTeamColors = new List<Color>(allTeamColorOptions);

        TeamColorButton.OnColorButtonSelected += HandleTeamColorSelected;
    }

    public override void OnStopServer()
    {
        TeamColorButton.OnColorButtonSelected -= HandleTeamColorSelected;
    }

    [Command]
    private void CmdSelectTeamColor(GameObject colorButtonGO)
    {
        TeamColorButton colorButton = colorButtonGO.GetComponent<TeamColorButton>();  // Custom type not supported by Mirror
        Color teamColor = colorButton.TeamColor;
        if (!availableTeamColors.Contains(teamColor)) { return; }  // Validation

        if (hasSelectedTeamColor)
        {
            availableTeamColors.Add(selectedTeamColor);
        }
        selectedTeamColor = teamColor;
        colorButton.HandleColorPlayerSelected();
        availableTeamColors.Remove(teamColor);
        hasSelectedTeamColor = true;
    }

    #endregion

    #region Client

    private void HandleTeamColorSelected(TeamColorButton colorButton, int playerConnectionId)
    {
        // Need authority check to prevent players who didn't select a color from running command?
        Debug.Log(playerConnectionId);
        Debug.Log(connectionToServer.connectionId);
        // Debug.Log($"HandleTeamColorSelected: playerConnectionId {playerConnectionId}, connectionToServer.connectionId {connectionToServer.connectionId}");
        if (playerConnectionId != connectionToServer.connectionId) { return; }

        CmdSelectTeamColor(colorButton.gameObject);
    }

    private void HandleAvailableTeamColorsUpdated(List<Color> oldAvailableColors, List<Color> newAvailableColors)
    {
        if (oldAvailableColors == null) { return; }  // Upon oldAvailableColors initialization

        foreach (TeamColorButton colorButton in colorButtons)  // Optimize?
        {
            Color teamColor = colorButton.TeamColor;
            if (oldAvailableColors.Contains(teamColor) && !newAvailableColors.Contains(teamColor))
            {
                colorButton.HandleColorMadeUnavailable();
            }
            else if (!oldAvailableColors.Contains(teamColor) && newAvailableColors.Contains(teamColor))
            {
                colorButton.HandleColorMadeAvailable();
            }
        }
    }

    #endregion
}
