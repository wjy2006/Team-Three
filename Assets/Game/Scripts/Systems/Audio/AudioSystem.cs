using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioSystem : MonoBehaviour
{
    [Header("Mixer & Channels")]
    public AudioMixer masterMixer;
    public AudioSource sourceA;
    public AudioSource sourceB;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 确保无视时间暂停，并且初始音量为0
        if (sourceA) { sourceA.ignoreListenerPause = true; sourceA.volume = 0f; }
        if (sourceB) { sourceB.ignoreListenerPause = true; sourceB.volume = 0f; }
    }

    private void Update()
    {
        UpdateGlitchAudioEffects();
    }

    public void PlayMusic(AudioClip clip, float duration = 1.0f, bool loop = true)
    {
        if (clip == null)
        {
            StopMusic(duration);
            return;
        }

        AudioSource targetSource = null;
        AudioSource fadingSource = null;

        // 1. 动态分配：谁有我们要的歌，谁就是 Target
        if (sourceA.clip == clip) 
        { 
            targetSource = sourceA; 
            fadingSource = sourceB; 
        }
        else if (sourceB.clip == clip) 
        { 
            targetSource = sourceB; 
            fadingSource = sourceA; 
        }
        else 
        {
            // 如果两个都没有这首歌，挑一个当前音量最小的（存在感最低的）来换碟片
            if (sourceA.volume <= sourceB.volume) 
            { 
                targetSource = sourceA; 
                fadingSource = sourceB; 
            }
            else 
            { 
                targetSource = sourceB; 
                fadingSource = sourceA; 
            }

            targetSource.clip = clip;
            targetSource.loop = loop;
        }

        // 2. 如果目标音箱已经拉满且正在播放，并且另一个彻底闭嘴了，直接无视（避免重复触发）
        if (targetSource.isPlaying && targetSource.volume >= 1f && fadingSource.volume <= 0f) 
        {
            return;
        }

        // 3. 确保目标音箱在播放状态
        if (!targetSource.isPlaying) 
        {
            targetSource.Play();
        }

        // 4. 打断旧的渐变，基于【当前实际音量】立刻开始新的渐变
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(CrossfadeRoutine(targetSource, fadingSource, duration));
    }

    public void StopMusic(float duration = 1.0f)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutBothRoutine(duration));
    }

    private IEnumerator CrossfadeRoutine(AudioSource target, AudioSource fading, float duration)
    {
        // 记录被打断时的瞬间音量，从这里接着过度
        float startTargetVol = target.volume;
        float startFadingVol = fading.volume;
        float elapsed = 0f;

        if (duration <= 0f) elapsed = duration;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            target.volume = Mathf.Lerp(startTargetVol, 1f, t);
            fading.volume = Mathf.Lerp(startFadingVol, 0f, t);
            yield return null;
        }

        target.volume = 1f;
        fading.volume = 0f;
        
        // 只有当 fading 音箱音量彻底降为 0 时，才停止它（节省性能）
        if (fading.isPlaying) 
        {
            fading.Stop();
        }
    }

    private IEnumerator FadeOutBothRoutine(float duration)
    {
        float startA = sourceA.volume;
        float startB = sourceB.volume;
        float elapsed = 0f;

        if (duration <= 0f) elapsed = duration;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            sourceA.volume = Mathf.Lerp(startA, 0f, t);
            sourceB.volume = Mathf.Lerp(startB, 0f, t);
            yield return null;
        }

        sourceA.volume = 0f;
        sourceB.volume = 0f;
        sourceA.Stop();
        sourceB.Stop();
    }

    private void UpdateGlitchAudioEffects()
    {
        if (GameRoot.I == null || masterMixer == null) return;
        float targetCutoff = GameRoot.I.IsGlitchWorld ? 900f : 22000f;
        float targetDist = GameRoot.I.IsGlitchWorld ? 0.4f : 0f;

        float curCutoff;
        if (masterMixer.GetFloat("GlitchCutoff", out curCutoff))
            masterMixer.SetFloat("GlitchCutoff", Mathf.Lerp(curCutoff, targetCutoff, Time.unscaledDeltaTime * 5f));
        
        float curDist;
        if (masterMixer.GetFloat("GlitchDist", out curDist))
            masterMixer.SetFloat("GlitchDist", Mathf.Lerp(curDist, targetDist, Time.unscaledDeltaTime * 5f));
    }
}