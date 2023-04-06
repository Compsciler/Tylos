using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public TMP_Text timerText;

    [Scene]
    [SerializeField] private string mainMenuScene;
    [SerializeField] private Animator fadeOutAnimator;

    private float _timer = 0;
    private float _startTime;

    MyNetworkManager _networkManager;

    void Awake()
    {
        _networkManager = ((MyNetworkManager)NetworkManager.singleton);
    }

    void Start()
    {
        // _startTime = ((MyNetworkManager)NetworkManager.singleton).LobbyCreatedTime;
    }

    void Update()
    {
        // _timer = (float)NetworkTime.time - _startTime;
        _timer = _networkManager.LobbyTimer;
        int minutes = Mathf.FloorToInt(_timer / 60);
        int seconds = Mathf.FloorToInt(_timer % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadMainMenuWithFadeOut());
    }

    private IEnumerator LoadMainMenuWithFadeOut()
    {
        fadeOutAnimator.SetBool("FadeOut", true);
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(mainMenuScene);
    }

}