using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
[VolumeComponentMenu("Custom/Glitch Effect")]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class GlitchVolume : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    public bool IsActive() => intensity.value > 0.001f;
    public bool IsTileCompatible() => false;
}