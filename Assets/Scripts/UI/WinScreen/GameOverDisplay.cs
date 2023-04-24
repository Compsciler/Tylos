using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class GameOverDisplay : MonoBehaviour
{
    [SerializeField] GameObject gameOverDisplayParent;

    [SerializeField] GameObject winDisplayParent;
    [SerializeField] GameObject loseDisplayParent;


    // [SerializeField] TMP_Text winnerNameText;

    private void Start()
    {
        GameOverHandler.TargetClientOnPlayerLost += TargetClientHandlePlayerLost;
        GameOverHandler.TargetClientOnPlayerWon += TargetClientHandlePlayerWon;
        GameOverHandler.ClientOnGameOver += ClientHandleGameOver;
    }

    private void OnDestroy()
    {
        GameOverHandler.TargetClientOnPlayerLost -= TargetClientHandlePlayerLost;
        GameOverHandler.TargetClientOnPlayerWon -= TargetClientHandlePlayerWon;
        GameOverHandler.ClientOnGameOver -= ClientHandleGameOver;
    }

    public void LeaveGame()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }

    private void TargetClientHandlePlayerLost()
    {
        DisplayLoseScreen();
    }

    private void TargetClientHandlePlayerWon()
    {
        DisplayWinScreen();
    }

    private void ClientHandleGameOver(string winner)
    {
        // winnerNameText.text = $"{winner} gets the victory royale";
        Debug.Log($"{winner} gets the victory royale");

        // gameOverDisplayParent.SetActive(true);
    }

    private void DisplayWinScreen()
    {
        gameOverDisplayParent.SetActive(true);
        winDisplayParent.SetActive(true);
        loseDisplayParent.SetActive(false);
    }

    private void DisplayLoseScreen()
    {
        gameOverDisplayParent.SetActive(true);
        loseDisplayParent.SetActive(true);
        winDisplayParent.SetActive(false);
    }
}
