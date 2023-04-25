using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] musicTracks;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
        PlayTrack(1);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void PlayTrack(int trackIndex)
    {
        if (trackIndex >= 0 && trackIndex < musicTracks.Length)
        {
            audioSource.clip = musicTracks[trackIndex];
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    public void UnPauseMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "LobbyScene")
        {
            PlayTrack(0);
        }

        if (scene.name == "GameScene")
        {
            PlayTrack(1);
        }

        if (scene.name == "MainMenuScene")
        {
            PauseMusic();
        }
        
    }
}