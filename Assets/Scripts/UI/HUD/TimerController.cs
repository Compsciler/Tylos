using Mirror;
using TMPro;
using UnityEngine;

public class TimerController : MonoBehaviour
{
    public TMP_Text timerText;
    private float _timer;
    MyNetworkManager _networkManager;

    void Awake()
    {
        _networkManager = ((MyNetworkManager)NetworkManager.singleton);
    }

    void Update()
    {
        // _timer = (float)NetworkTime.time - _startTime;
        _timer = _networkManager.LobbyTimer;
        int minutes = Mathf.FloorToInt(_timer / 60);
        int seconds = Mathf.FloorToInt(_timer % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
