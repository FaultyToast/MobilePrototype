using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeInOut : MonoBehaviour
{
    public CanvasGroup canvasToFade;

    public float fadeInTime = 1f;
    public float fadeOutTime = 1f;
    public float holdTime = 1f;

    public void BeginFade()
    {
        StopAllCoroutines();
        StartCoroutine(FadeInOutCoroutine());
    }

    private IEnumerator FadeInOutCoroutine()
    {
        float timer = 0;
        while (timer < fadeInTime)
        {
            timer += Time.deltaTime;
            timer = Mathf.Min(timer, fadeInTime);
            canvasToFade.alpha = Mathf.InverseLerp(0, fadeInTime, timer);
            yield return null;
        }

        yield return new WaitForSeconds(holdTime);

        timer = fadeOutTime;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            timer = Mathf.Max(timer, 0);
            canvasToFade.alpha = Mathf.InverseLerp(0, fadeOutTime, timer);
            yield return null;
        }
    }
}
