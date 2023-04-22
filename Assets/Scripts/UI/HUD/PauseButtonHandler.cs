using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseButtonHandler : MonoBehaviour
{
    public Button pauseButton;
    public GameObject pauseScreen;
    public float fadeDuration = 0.5f;
    public Button exitGameButton;
    public Image fadeOverlay;
    public GameObject letters;


    private bool _isPaused = false;
    private CanvasGroup _pauseScreenCanvasGroup;
    private CanvasGroup _lettersCanvasGroup;


    private void Start()
    {
        pauseButton.onClick.AddListener(TogglePause);
        exitGameButton.onClick.AddListener(ExitGame);
        _pauseScreenCanvasGroup = pauseScreen.GetComponent<CanvasGroup>();
        _lettersCanvasGroup = letters.GetComponent<CanvasGroup>();
        pauseScreen.SetActive(false);
    }

    public void TogglePause()
    {
        _isPaused = !_isPaused;
        StartCoroutine(FadePauseScreen(_isPaused));
    }
    
    private void ExitGame()
    {
        StartCoroutine(FadeAndLoadScene("MainMenuScene"));
    }

    private IEnumerator FadePauseScreen(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;

        if (fadeIn)
        {
            pauseScreen.SetActive(true);
        }

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            _pauseScreenCanvasGroup.alpha = currentAlpha;
            yield return null;
        }

        if (!fadeIn)
        {
            pauseScreen.SetActive(false);
        }
    }
    
    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        // Perform the fade-out effect
        float elapsedTime = 0f;
        float startAlpha = 0f;
        float endAlpha = 1f;
        Color fadeColor = fadeOverlay.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / fadeDuration);
            fadeOverlay.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, currentAlpha);
            _lettersCanvasGroup.alpha = currentAlpha;
            yield return null;
        }

        // Load the main menu scene
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    
}
