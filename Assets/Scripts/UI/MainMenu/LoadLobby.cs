using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadLobby : MonoBehaviour
{
    public Animator fadeOverlayAnimator;
    public Animator switchButtonsAnimator;

    [Scene]
    [SerializeField] string lobbyScene;

    private static readonly int FadeOutTrigger = Animator.StringToHash("FadeOutTrigger");
    private static readonly int SwitchButtonsTrigger = Animator.StringToHash("SwitchButtonsTrigger");

    public void OnJoinButtonClick()
    {
        StartCoroutine(LoadLobbyWithFade());
    }

    public void OnPlayButtonClick()
    {
        switchButtonsAnimator.SetTrigger(SwitchButtonsTrigger);
    }

    public void OnHostButtonClick()
    {
        StartCoroutine(LoadLobbyWithFade());
        NetworkManager.singleton.StartHost();
    }


    IEnumerator LoadLobbyWithFade()
    {
        fadeOverlayAnimator.SetTrigger(FadeOutTrigger);
        yield return new WaitForSeconds(1.0f); // Adjust this value to match the duration of your fade-out animation.
        SceneManager.LoadScene(lobbyScene);
    }

}