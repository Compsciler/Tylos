using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleController : MonoBehaviour
{
    [SerializeField] private Animator transitionAnimator;

    [Scene]
    [SerializeField] string mainMenuScene;

    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        transitionAnimator.SetTrigger("FadeOutTrigger");
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(sceneName);
    }

    public void OpenNextScreen()
    {
        StartCoroutine(LoadSceneWithTransition(mainMenuScene));
    }
}
