// Minimal URP-compatible shader that renders vertex colors.
// Used for solid terrain blocks (grass, dirt, stone, sand).
// No textures needed – colors come from mesh vertex data.
// v0.5.7: Receives shadows from directional light.
Shader "Terranova/VertexColorOpaque"
{
    Properties
    {
        // No properties needed – everything comes from vertex colors
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            // Include URP core functions (object-to-clip transforms, etc.)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;   // Object-space position
                float3 normalOS   : NORMAL;     // Object-space normal
                float4 color      : COLOR;      // Vertex color (our block color)
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;  // Clip-space position
                float4 color       : COLOR;         // Passed to fragment shader
                float3 normalWS    : TEXCOORD0;    // World-space normal for lighting
                float3 positionWS  : TEXCOORD1;    // For shadow coord computation
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // v0.5.7: Sample shadow map for directional light
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float NdotL = saturate(dot(normalize(input.normalWS), mainLight.direction));

                // Combine vertex color with basic lighting (ambient + diffuse)
                float3 ambient = 0.3;
                float3 diffuse = NdotL * mainLight.color.rgb * mainLight.shadowAttenuation * 0.7;
                float3 finalColor = input.color.rgb * (ambient + diffuse);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Shadow caster pass so terrain casts shadows
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            // Need both Core and Lighting for shadow bias + light direction
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normalWS, GetMainLight().direction));
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
