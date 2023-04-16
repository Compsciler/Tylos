using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuitButtonHandler : MonoBehaviour
{
    private Button quitButton;

    void Start()
    {
        quitButton = GetComponent<Button>();
        quitButton.onClick.AddListener(QuitGame);
    }

    void QuitGame()
    {
        Application.Quit();
    }
}
