using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyMenu : MonoBehaviour
{
    // [SerializeField] GameObject lobbyUI;
    [SerializeField] Button startGameButton;
    [SerializeField] GameObject[] playerCards;
    [SerializeField] TMP_Text[] playerNameTexts;

    [SerializeField] LobbyController lobbyController;

    string waitingForPlayerText;

    [SerializeField] Color queuedPlayerColor;
    [SerializeField] Color waitingForPlayerColor;

    void Awake()
    {
        waitingForPlayerText = playerNameTexts[0].text;

        MyNetworkManager.ClientOnConnected += HandleClientConnected;
        MyPlayer.AuthorityOnPartyOwnerStateUpdated += AuthorityHandlePartyOwnerStateUpdated;
        MyPlayer.ClientOnInfoUpdated += ClientHandleInfoUpdated;
    }

    void OnDestroy()
    {
        MyNetworkManager.ClientOnConnected -= HandleClientConnected;
        MyPlayer.AuthorityOnPartyOwnerStateUpdated -= AuthorityHandlePartyOwnerStateUpdated;
        MyPlayer.ClientOnInfoUpdated -= ClientHandleInfoUpdated;
    }

    private void HandleClientConnected()
    {
        // lobbyUI.SetActive(true);
    }

    // TODO: make sure LobbyMenu is DontDestroyOnLoad from MainMenu
    private void ClientHandleInfoUpdated()
    {
        List<MyPlayer> players = ((MyNetworkManager)NetworkManager.singleton).Players;

        for (int i = 0; i < players.Count && i < playerNameTexts.Length; i++)
        {
            SetPlayerQueued(i, players);
        }

        for (int i = players.Count; i < playerNameTexts.Length; i++)
        {
            SetPlayerWaiting(i, players);
        }

        Debug.Log($"Player count: {players.Count}");
        startGameButton.interactable = players.Count >= 2;
    }

    private void SetPlayerQueued(int playerIndex, List<MyPlayer> players)
    {
        Debug.Log("here 1");
        playerNameTexts[playerIndex].text = players[playerIndex].DisplayName;
        playerCards[playerIndex].GetComponent<RawImage>().color = queuedPlayerColor;
        Debug.Log("Player " + playerIndex + " is queued");
    }

    private void SetPlayerWaiting(int playerIndex, List<MyPlayer> players)
    {
        playerNameTexts[playerIndex].text = waitingForPlayerText;
        playerCards[playerIndex].GetComponent<RawImage>().color = waitingForPlayerColor;
        Debug.Log("Player " + playerIndex + " is waiting");
    }

    private void AuthorityHandlePartyOwnerStateUpdated(bool state)
    {
        startGameButton.gameObject.SetActive(state);
    }

    public void StartGame()
    {
        NetworkClient.connection.identity.GetComponent<MyPlayer>().CmdStartGame();
    }

    public void LeaveLobby()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();

            lobbyController.LoadMainMenu();
        }
    }
}
