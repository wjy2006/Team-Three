Shader "Hidden/URPGlitch"
{
    Properties
    {
        _Intensity("Intensity", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }
        LOD 100
        ZWrite Off Cull Off

        Pass
        {
            Name "GlitchPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Unity 6 必须包含这个库来获取 Blitter 相关定义
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float _Intensity;
            float _TimeX;

            // 简单的噪声函数
            float random(float2 st) 
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 设置立体渲染（如果是 VR 也是兼容的）
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float strength = _Intensity;

                // 1. 屏幕横向撕裂效果
                float glitchLine = step(0.9, random(float2(floor(uv.y * 20.0), _TimeX * 10.0)));
                float offsetX = glitchLine * 0.05 * strength * random(float2(_TimeX, uv.y));
                
                // 2. RGB 分离 (Chromatic Aberration)
                float splitOffset = 0.02 * strength;
                float2 uvR = uv + float2(offsetX + splitOffset, 0);
                float2 uvG = uv + float2(offsetX, 0);
                float2 uvB = uv + float2(offsetX - splitOffset, 0);

                // 3. 使用 SAMPLE_TEXTURE2D_X 进行采样
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvR).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvG).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvB).b;
                half a = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uvG).a;

                // 4. 叠加一些随机噪点
                float noise = (random(uv + _TimeX) - 0.5) * 0.1 * strength;
                
                return half4(r + noise, g + noise, b + noise, a);
            }
            ENDHLSL
        }
    }
}