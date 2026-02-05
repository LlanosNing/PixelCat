Shader "Toon/ToonShader"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        
        [Header(Toon Lighting)]
        _Step ("Toon Step", Range(0.01, 1)) = 0.5
        _Smoothing ("Toon Smoothing", Range(0, 1)) = 0.05
        _ShadowColorTint ("Shadow Tint", Color) = (0.5, 0.5, 0.5, 1)
        
        [Header(Outline)]
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02
        _OutlineBrightness ("Outline Brightness (Tint)", Range(0, 1)) = 0.3
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        // ------------------------------------------------------------------
        // Pass 1: Outline (Hull Expansion)
        // ------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front // Render only back faces for hull outline
            ZWrite On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            float _OutlineWidth;
            float _OutlineBrightness;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _BaseColor;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Expland along normal for outline
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                
                // Offset position along normal
                positionWS += normalWS * _OutlineWidth;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample texture at current UV to match object color
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 color = texColor * _BaseColor;
                
                // Darken the outline based on the sampled color
                color.rgb *= _OutlineBrightness;
                
                return color;
            }
            ENDHLSL
        }

        // ------------------------------------------------------------------
        // Pass 2: Main Toon Forward Pass
        // ------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD3;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _BaseColor;
            float _Step;
            float _Smoothing;
            float4 _ShadowColorTint;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Light data
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float d = dot(normal, mainLight.direction);
                
                // Toon lighting step
                float toonL = smoothstep(_Step - _Smoothing, _Step + _Smoothing, d * 0.5 + 0.5);
                
                // Combine colors
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                float3 lightColor = lerp(_ShadowColorTint.rgb, mainLight.color, toonL);
                
                return half4(albedo.rgb * lightColor, albedo.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
