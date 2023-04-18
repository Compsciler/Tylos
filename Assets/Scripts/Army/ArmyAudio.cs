using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror; 

public class ArmyAudio : NetworkBehaviour
{
    [SerializeField] private AudioSource attackAudioSource;

    public void PlayAttackAudio()
    {
        attackAudioSource.Play();
    }

    public void StopAttackAudio()
    {
        attackAudioSource.Stop();
    }

    public void StopAudio() // Stop all audio
    {
        StopAttackAudio();
    }
}
