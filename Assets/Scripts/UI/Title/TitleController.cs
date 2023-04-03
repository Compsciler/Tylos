using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
public class TitleController : MonoBehaviour
{
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private Animator titleTextAnimator;
    [Scene]
    [SerializeField] string mainMenuScene;

    private static readonly int FadeOutTrigger = Animator.StringToHash("FadeOutTrigger");
    private static readonly int PlayTransition = Animator.StringToHash("PlayTransition");

    private IEnumerator LoadSceneWithTransition(string sceneName)
    {
        // Set the FadeOutTrigger parameter to true
        transitionAnimator.SetBool(FadeOutTrigger, true);

        // Set the PlayTransition parameter to true
        titleTextAnimator.SetBool(PlayTransition, true);

        // Play the title transition animation
        titleTextAnimator.Play("TitleTextMove");

        // Wait for the title transition animation to complete before loading the next scene
        // Replace 1f with the duration of your title transition animation
        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(sceneName);
    }

    public void OpenNextScreen()
    {
        StartCoroutine(LoadSceneWithTransition(mainMenuScene));
    }
}
