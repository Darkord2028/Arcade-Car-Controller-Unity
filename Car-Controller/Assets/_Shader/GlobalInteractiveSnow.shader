Shader "Custom/GlobalInteractiveSnow"
{
    Properties
    {
        _SnowColor("Snow Color", Color) = (1, 1, 1, 1)
        
        // How deep the footprints push the mesh down
        _VertexOffset("Snow Depth", Float) = 0.5 
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
            
            // Core URP library
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
            };

            // Standard material properties
            CBUFFER_START(UnityPerMaterial)
                half4 _SnowColor;
                float _VertexOffset;
            CBUFFER_END

            // --- GLOBAL VARIABLES ---
            // These do NOT go in the Properties block at the top.
            // By declaring them here, they automatically listen to Shader.SetGlobalTexture() in C#
            TEXTURE2D(_PathTexture);
            SAMPLER(sampler_PathTexture);
            float _SnowWorldSize;

            // 1. VERTEX SHADER (Handles the physical deformation)
            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Convert object local position to absolute World Space
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                // Map World Position to 0.0 - 1.0 Global UV Space.
                // We add 0.5 so the center of the texture sits at world origin (0,0,0)
                float2 globalUV = (positionWS.xz / _SnowWorldSize) + 0.5;

                // Sample the global Render Texture (using LOD because standard sampling isn't allowed in vertex shaders)
                half snowData = SAMPLE_TEXTURE2D_LOD(_PathTexture, sampler_PathTexture, globalUV, 0).r;

                // Calculate downward offset based on the texture's RED channel (0.0 to 1.0)
                float3 offsetWS = TransformObjectToWorldDir(IN.normalOS) * (snowData * _VertexOffset);
                
                // Apply the deformation by subtracting the offset
                positionWS -= offsetWS;

                // Convert the newly deformed World Position into Screen Space for rendering
                OUT.positionHCS = TransformWorldToHClip(positionWS);
                OUT.positionWS = positionWS;
                
                return OUT;
            }

            // 2. FRAGMENT SHADER (Handles the color)
            half4 frag(Varyings IN) : SV_Target
            {
                // Recalculate the UVs in the pixel shader to read the texture again
                float2 globalUV = (IN.positionWS.xz / _SnowWorldSize) + 0.5;
                half pathValue = SAMPLE_TEXTURE2D(_PathTexture, sampler_PathTexture, globalUV).r;

                // Base snow color
                half3 finalColor = _SnowColor.rgb;

                // Bonus effect: Slightly darken the snow wherever it is crushed 
                // to fake ambient occlusion/shadows inside the footprints.
                // It blends from 1.0 (white surface) to 0.7 (darker footprint).
                finalColor *= lerp(1.0, 0.7, pathValue);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}