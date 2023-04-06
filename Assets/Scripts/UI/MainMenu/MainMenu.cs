using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Scene]
    [SerializeField] string lobbyScene;

    public void HostLobby()
    {
        SceneManager.LoadScene(lobbyScene);

        NetworkManager.singleton.StartHost();
    }
}
