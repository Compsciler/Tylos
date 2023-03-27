using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadLobby : MonoBehaviour
{
    public Animator fadeOverlayAnimator;

    public void OnPlayButtonClick()
    {
        StartCoroutine(LoadLobbyWithFade());
    }

    IEnumerator LoadLobbyWithFade()
    {
        fadeOverlayAnimator.SetTrigger("FadeOutTrigger");
        yield return new WaitForSeconds(1); // Adjust this value to match the duration of your fade-out animation.
        SceneManager.LoadScene("LobbyScene");
    }
}