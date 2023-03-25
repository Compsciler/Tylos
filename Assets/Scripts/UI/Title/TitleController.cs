using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleController : MonoBehaviour
{
    [SerializeField] private Animator transitionAnimator;

    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        transitionAnimator.SetTrigger("PlayTransition");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(sceneName);
    }
    
    public void OpenNextScreen()
    {
        StartCoroutine(LoadSceneWithTransition("MainMenuScene"));
    }
    
}
