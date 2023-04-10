using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinCanvasController : MonoBehaviour
{
    
    [SerializeField] private Animator maskAnimator;
    [SerializeField] private string victoryAnimationName = "UnrollBanner";

    private void Start()
    {
        TriggerVictory();
    }

    public void TriggerVictory()
    {
        maskAnimator.Play(victoryAnimationName);
    }
}
