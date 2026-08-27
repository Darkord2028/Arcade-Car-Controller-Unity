Shader "Custom/GlobalInteractiveSnow"
{
    Properties
    {
        [HDR] _SnowColor("Snow Color", Color) = (1.5, 1.5, 1.5, 1)
        _VertexOffset("Vertex Offset (Depth)", Float) = 0.5
        
        [Space(10)]
        _GlobalNoiseScale("Global Noise Tiling (Size)", Float) = 1.0
        _NoiseIntensity("Noise Height Multiplier", Range(0, 3)) = 1.0
        _MacroScale("Macro Noise Scale", Float) = 5.0
        _MicroScale("Micro Noise Scale", Float) = 50.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float totalHeight   : TEXCOORD1; 
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _SnowColor;
                float _VertexOffset;
                float _GlobalNoiseScale;
                float _NoiseIntensity;
                float _MacroScale;
                float _MicroScale;
            CBUFFER_END

            TEXTURE2D(_PathTexture);
            SAMPLER(sampler_PathTexture);
            float _SnowWorldSize; 

            float hash31(float3 p3)
            {
                p3  = frac(p3 * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Unity_SimpleNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                
                float3 u = f * f * (3.0 - 2.0 * f);

                float a = hash31(i + float3(0, 0, 0));
                float b = hash31(i + float3(1, 0, 0));
                float c = hash31(i + float3(0, 1, 0));
                float d = hash31(i + float3(1, 1, 0));
                float e = hash31(i + float3(0, 0, 1));
                float f1 = hash31(i + float3(1, 0, 1));
                float g = hash31(i + float3(0, 1, 1));
                float h = hash31(i + float3(1, 1, 1));

                return lerp(lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y),
                            lerp(lerp(e, f1, u.x), lerp(g, h, u.x), u.y), u.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                float2 globalUV = (positionWS.xz / _SnowWorldSize) + 0.5;
                float rtData = SAMPLE_TEXTURE2D_LOD(_PathTexture, sampler_PathTexture, globalUV, 0).r;
                float untouchedSnow = 1.0 - rtData; 
                
                float3 noisePos = positionWS * _GlobalNoiseScale;

                float macroNoise = Unity_SimpleNoise3D(noisePos * _MacroScale) * 0.2;
                float microNoise = Unity_SimpleNoise3D(noisePos * _MicroScale);
                
                float combinedNoise = (macroNoise + microNoise) * _NoiseIntensity;
                
                combinedNoise = smoothstep(0.2, 0.8, combinedNoise);

                float maskedNoise = combinedNoise * untouchedSnow;
                float totalHeight = maskedNoise + untouchedSnow;

                positionWS += float3(0, 1, 0) * (totalHeight * _VertexOffset);

                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;
                OUT.totalHeight = totalHeight; 
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float colorMultiplier = max(IN.totalHeight, 0.5); 
                half3 finalColor = _SnowColor.rgb * colorMultiplier;
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}