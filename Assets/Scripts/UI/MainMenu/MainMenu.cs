using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Scene]
    [SerializeField] string mainMenuScene;
    [Scene]
    [SerializeField] string lobbyScene;

    bool useSteam;
    int maxConnections;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    const string HostAddressKey = "HostAddress";


    void Start()
    {
        useSteam = ((MyNetworkManager)MyNetworkManager.singleton).UseSteam;
        maxConnections = ((MyNetworkManager)MyNetworkManager.singleton).MaxConnections;

        if (!useSteam) { return; }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    void Update()
    {
        if (!useSteam) { return; }

        if (Input.GetButtonDown("Jump"))
        {
            ((MyNetworkManager)NetworkManager.singleton).StartGame();
        }
    }

    public void HostLobby()
    {
        // TODO: Add a loading screen

        if (useSteam)
        {
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxConnections);
            return;
        }

        SceneManager.LoadScene(lobbyScene);

        NetworkManager.singleton.StartHost();
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            SceneManager.LoadScene(mainMenuScene);
            return;
        }

        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        MyNetworkManager.LobbyId = lobbyId.m_SteamID;

        NetworkManager.singleton.StartHost();

        SteamMatchmaking.SetLobbyData(
            lobbyId,
            HostAddressKey,
            SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active) { return; }

        string hostAddress = SteamMatchmaking.GetLobbyData(
            new CSteamID(callback.m_ulSteamIDLobby),
            HostAddressKey);

        NetworkManager.singleton.networkAddress = hostAddress;
        NetworkManager.singleton.StartClient();

        SceneManager.LoadScene(lobbyScene);
    }
}
