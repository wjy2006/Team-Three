Shader "Game/Sprite Glitch Zone"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0

        _Intensity("Intensity", Range(0, 1)) = 0.75
        _RGBSplit("RGB Split", Range(0, 0.05)) = 0.012
        _SliceStrength("Slice Strength", Range(0, 0.08)) = 0.025
        _SliceCount("Slice Count", Range(1, 256)) = 48
        _BlockStrength("Block Strength", Range(0, 0.05)) = 0.02
        _BlockGrid("Block Grid", Vector) = (18, 14, 0, 0)
        _NoiseStrength("Noise Strength", Range(0, 0.2)) = 0.05
        _Flash("Flash", Range(0, 1)) = 0.1
        _TimeScale("Time Scale", Range(0, 4)) = 1
        _Seed("Seed", Float) = 0

        [MaterialToggle] _UseWorldZone("Use World Zone", Float) = 0
        _ZoneCenter("Zone Center", Vector) = (0, 0, 0, 0)
        _ZoneSize("Zone Size", Vector) = (1, 1, 0, 0)
        _ZoneSoftness("Zone Softness", Float) = 0.15

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex SpriteGlitchVertex
            #pragma fragment SpriteGlitchFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_AlphaTex);
            SAMPLER(sampler_AlphaTex);

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
                float3 worldPos : TEXCOORD4;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _RGBSplit;
                half _SliceStrength;
                half _SliceCount;
                half _BlockStrength;
                half4 _BlockGrid;
                half _NoiseStrength;
                half _Flash;
                half _TimeScale;
                half _Seed;
                half _UseWorldZone;
                half4 _ZoneCenter;
                half4 _ZoneSize;
                half _ZoneSoftness;
                half _EnableExternalAlpha;
            CBUFFER_END

            half Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash21(p + 19.19));
            }

            half4 SampleSpriteTexture(float2 uv)
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                if (_EnableExternalAlpha > 0.5h)
                {
                    half alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, uv).r;
                    color.a = alpha;
                }

                return color;
            }

            half GetZoneMask(float2 worldPos)
            {
                if (_UseWorldZone < 0.5h)
                    return 1.0h;

                float2 halfSize = max(_ZoneSize.xy * 0.5, float2(0.0001, 0.0001));
                float2 edge = abs(worldPos - _ZoneCenter.xy) - halfSize;
                float outside = max(edge.x, edge.y);
                float softness = max(_ZoneSoftness, 0.0001h);
                return 1.0h - smoothstep(0.0, softness, outside);
            }

            Varyings SpriteGlitchVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.positionCS = TransformObjectToHClip(input.positionOS);
#if defined(DEBUG_DISPLAY)
                o.positionWS = TransformObjectToWorld(input.positionOS);
#endif
                o.worldPos = TransformObjectToWorld(input.positionOS);
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 SpriteGlitchFragment(Varyings input) : SV_Target
            {
                half4 baseColor = SampleSpriteTexture(input.uv) * input.color;

#if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(baseColor.rgb, baseColor.a, surfaceData);
                InitializeInputData(input.uv, inputData);
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, input.positionWS, input.positionCS, _MainTex);

                if (CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
#endif

                if (baseColor.a <= 0.001h)
                    return baseColor;

                half zoneMask = GetZoneMask(input.worldPos.xy);
                half glitch = saturate(_Intensity) * zoneMask;
                if (glitch <= 0.001h)
                    return baseColor;

                float time = _Time.y * max(_TimeScale, 0.0h) + (_Seed * 17.17h);

                float lineId = floor(input.worldPos.y * max(_SliceCount, 1.0h));
                float sliceNoise = Hash21(float2(lineId, floor(time * 12.0)));
                float sliceMask = step(0.62, sliceNoise) * glitch;
                float sliceOffset = (Hash21(float2(lineId + 11.4, floor(time * 16.0))) * 2.0 - 1.0) * _SliceStrength * sliceMask;

                float2 grid = max(_BlockGrid.xy, float2(1.0, 1.0));
                float2 blockCell = floor(input.uv * grid);
                float2 blockRand = Hash22(blockCell + floor(time * 20.0) + _Seed);
                float2 blockOffset = (blockRand - 0.5) * _BlockStrength * glitch;

                float split = _RGBSplit * glitch * (0.35 + 0.65 * Hash21(float2(floor(time * 24.0), lineId + _Seed)));
                float2 baseOffset = float2(sliceOffset, 0.0) + blockOffset;

                half4 sampleR = SampleSpriteTexture(input.uv + baseOffset + float2(split, 0.0)) * input.color;
                half4 sampleG = SampleSpriteTexture(input.uv + baseOffset) * input.color;
                half4 sampleB = SampleSpriteTexture(input.uv + baseOffset - float2(split, 0.0)) * input.color;

                half4 glitched = half4(sampleR.r, sampleG.g, sampleB.b, max(max(sampleR.a, sampleG.a), sampleB.a));

                float noise = (Hash21(input.uv * 128.0 + float2(time * 7.0, time * 11.0)) - 0.5) * _NoiseStrength * glitch;
                float flashMask = step(0.9, Hash21(float2(floor(time * 9.0), floor(input.worldPos.x * 6.0 + input.worldPos.y * 9.0) + _Seed)));
                float flash = flashMask * _Flash * glitch;

                glitched.rgb += noise.xxx + flash.xxx;
                glitched.rgb = max(glitched.rgb, 0.0);

                return lerp(baseColor, glitched, glitch);
            }
            ENDHLSL
        }
    }
}
