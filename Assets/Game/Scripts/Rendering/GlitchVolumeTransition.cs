using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class GlitchVolumeTransition : MonoBehaviour
{
    public float rampDown = 0.10f;
    
    // 把 DDOL 上的那个 Volume 拖进来
    public Volume targetVolume; 
    
    private GlitchVolume gv;

    void Awake()
    {
        // ✅ 核心：从 Profile 资源中获取具体的 GlitchVolume 实例
        if (targetVolume != null && targetVolume.profile != null)
        {
            targetVolume.profile.TryGet(out gv);
        }

        if (gv == null)
        {
            Debug.LogError("[Glitch] 在 Volume Profile 中没找到 GlitchVolume 组件！请确保你已经在 Profile 里添加了它。");
        }
        else
        {
            // 初始化设为 0，且确保 Volume 本身的权重是 1
            gv.intensity.value = 0f;
            targetVolume.weight = 1f; 
        }
    }

    public IEnumerator GlitchOut(float duration = 0.15f)
    {
        if (gv == null) yield break;
        
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // ✅ 直接改 Intensity，你会看到滑块在动
            gv.intensity.value = t * t; 
            yield return null;
        }
        gv.intensity.value = 1f;
    }

    public IEnumerator GlitchIn()
    {
        if (gv == null) yield break;

        float elapsed = 0;
        float startVal = gv.intensity.value;

        while (elapsed < rampDown)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / rampDown);
            
            // 平滑降回 0
            gv.intensity.value = Mathf.Lerp(startVal, 0f, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        gv.intensity.value = 0f;
    }
}