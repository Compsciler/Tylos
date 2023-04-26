using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugText : Singleton<DebugText>
{
    [SerializeField] TMP_Text debugText;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public string GetText()
    {
        return debugText.text;
    }

    public void SetText(string text)
    {
        debugText.text = text;
    }

    public void AppendText(string text)
    {
        debugText.text += text;
    }
}
