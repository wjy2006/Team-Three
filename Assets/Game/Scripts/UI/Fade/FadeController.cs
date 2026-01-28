using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public Image fadeImage;

    private Coroutine running;

    private void Awake()
    {
        if (fadeImage == null) fadeImage = GetComponentInChildren<Image>(true);
        SetAlpha(0f);
    }

    public void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        var c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
        fadeImage.raycastTarget = a > 0.001f; // 黑屏时挡住点击，避免误触
    }

    public IEnumerator FadeOut(float duration)
    {
        yield return FadeTo(1f, duration);
    }

    public IEnumerator FadeIn(float duration)
    {
        yield return FadeTo(0f, duration);
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(FadeRoutine(targetAlpha, duration));
        yield return running;
        running = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
{
    float startAlpha = fadeImage.color.a;

    if (duration <= 0f)
    {
        SetAlpha(targetAlpha);
        yield break;
    }

    float startTime = Time.unscaledTime;
    float endTime = startTime + duration;

    while (Time.unscaledTime < endTime)
    {
        float t = Mathf.InverseLerp(startTime, endTime, Time.unscaledTime);

        // 可选：更丝滑的缓动（比线性更好看）
        t = Mathf.SmoothStep(0f, 1f, t);

        float a = Mathf.Lerp(startAlpha, targetAlpha, t);
        SetAlpha(a);
        yield return null;
    }

    SetAlpha(targetAlpha);
}

}
