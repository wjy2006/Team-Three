Shader "Hidden/URPTransitionGlitch"
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
            Name "TransitionGlitchPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag

            float _Intensity;
            float _TimeX;

            // 基础随机函数
            float random(float2 st) {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            // 块状噪声：用于像素化崩坏
            float blockNoise(float2 seed, float size) {
                return random(floor(seed * size));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                
                float2 uv = input.texcoord;
                float t = _TimeX;
                float strength = _Intensity;

                // 1. 像素化/位移 (Blocky Distortion)
                // 强度越大，色块越碎
                float block = blockNoise(float2(uv.y, t), 10.0);
                float lineNoise = pow(block, 8.0) * pow(blockNoise(float2(uv.y, t), 100.0), 3.0);
                float offsetX = lineNoise * 0.2 * strength;
                
                // 2. 严重的 RGB 分离
                float split = 0.05 * strength * random(float2(t, 2.0));
                half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offsetX + split, 0)).r;
                half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offsetX, 0)).g;
                half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offsetX - split, 0)).b;
                half a = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(offsetX, 0)).a;

                // 3. 信号雪花 (Static Noise Overlay)
                // 当强度超过 0.7 时，雪花开始占据主导
                float staticNoise = random(uv + float2(t, t));
                half3 sceneCol = half3(r, g, b);
                
                // 混合雪花逻辑：强度越高，越倾向于灰白的电子噪点
                float staticWeight = smoothstep(0.5, 1.0, strength) * 0.7;
                sceneCol = lerp(sceneCol, half3(staticNoise, staticNoise, staticNoise), staticWeight);

                // 4. 扫描线效果 (Scanlines)
                float scanline = sin(uv.y * 400.0 + t * 10.0) * 0.04 * strength;
                sceneCol -= scanline;

                // 5. 瞬间闪烁 (Flicker)
                // 模拟信号即将断掉时的明暗跳变
                float flicker = 1.0 - (random(float2(t, t)) * 0.5 * strength);
                sceneCol *= flicker;

                return half4(sceneCol, a);
            }
            ENDHLSL
        }
    }
}