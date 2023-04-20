using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FadeIn : MonoBehaviour
{
    public GameObject fadeOverlay;
    public float fadeDuration = 0.5f;

    private CanvasGroup _canvasGroup;
    // Start is called before the first frame update
    void Start()
    {
        fadeOverlay.gameObject.SetActive(true);
        _canvasGroup = fadeOverlay.GetComponent<CanvasGroup>();
        StartCoroutine(Fade());
    }

    // Update is called once per frame
    void Update()
    {

    }

    private IEnumerator Fade()
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float currentAlpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            _canvasGroup.alpha = currentAlpha;
            yield return null;
        }
    }
}
