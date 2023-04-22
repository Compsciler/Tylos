using UnityEngine;
using Mirror; 

public class ArmyAudio : NetworkBehaviour
{
    public enum Sound{
        None,
        Attack
    }

    Sound currentSound;
    [SerializeField] private AudioSource attackAudioSource;

    public void PlayAttackAudio()
    {
        if(currentSound == Sound.Attack)
            return;
        currentSound = Sound.Attack;
        attackAudioSource.Play();
    }

    public void StopAttackAudio()
    {
        if(currentSound != Sound.Attack)
            return;
        currentSound = Sound.None; 
        attackAudioSource.Stop();
    }

    public void StopAudio() // Stop all audio
    {
        StopAttackAudio();
        currentSound = Sound.None;
    }
}
