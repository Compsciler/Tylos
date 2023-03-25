using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextTwinkle : MonoBehaviour
{
    public TMP_Text text;
    public float twinkleSpeed = 1.0f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1.0f;

    private void Start()
    {
        StartCoroutine(Twinkle());
    }

    private IEnumerator Twinkle()
    {
        while (true)
        {
            float alpha = (Mathf.Sin(Time.time * twinkleSpeed) + 1) / 2;
            alpha = Mathf.Lerp(minAlpha, maxAlpha, alpha);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            yield return null;
        }
    }
}
