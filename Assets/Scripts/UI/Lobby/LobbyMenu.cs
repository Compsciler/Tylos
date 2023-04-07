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
    [SerializeField] Canvas lobbyCanvas;
    public Canvas LobbyCanvas => lobbyCanvas;

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

        LobbySceneInitPlayerState();
    }

    void OnDestroy()
    {
        MyNetworkManager.ClientOnConnected -= HandleClientConnected;
        MyPlayer.AuthorityOnPartyOwnerStateUpdated -= AuthorityHandlePartyOwnerStateUpdated;
        MyPlayer.ClientOnInfoUpdated -= ClientHandleInfoUpdated;
    }

    private void LobbySceneInitPlayerState()
    {
        ClientHandleInfoUpdated();

        if (NetworkServer.active)  // Is server (party owner)
        {
            AuthorityHandlePartyOwnerStateUpdated(true);
        }
        else
        {
            AuthorityHandlePartyOwnerStateUpdated(false);
        }
    }

    private void HandleClientConnected()
    {
        // lobbyUI.SetActive(true);
    }

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

        startGameButton.interactable = players.Count >= 2 || ((MyNetworkManager)NetworkManager.singleton).CanStartWith1Player;
    }

    private void SetPlayerQueued(int playerIndex, List<MyPlayer> players)
    {
        playerNameTexts[playerIndex].text = players[playerIndex].DisplayName;
        playerCards[playerIndex].GetComponent<RawImage>().color = queuedPlayerColor;
    }

    private void SetPlayerWaiting(int playerIndex, List<MyPlayer> players)
    {
        playerNameTexts[playerIndex].text = waitingForPlayerText;
        playerCards[playerIndex].GetComponent<RawImage>().color = waitingForPlayerColor;
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
