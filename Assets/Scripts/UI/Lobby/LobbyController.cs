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
    
    private float _timer;

    void Start()
    {
        _timer = 0;
    }

    void Update()
    {
        _timer += Time.deltaTime;
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