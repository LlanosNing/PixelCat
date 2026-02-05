Shader "Hidden/PixelArt"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ColorSteps ("Color Steps", Float) = 8
        _DitherStrength ("Dither Strength", Float) = 0.2
        _Sharpness ("Sharpness", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        
        ZWrite Off
        ZTest Always
        Blend Off
        Cull Off
        
        // Pass 0: Downsample and Quantize
        Pass
        {
            Name "PixelArt Downsample"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            float _ColorSteps;
            float _DitherStrength;
            
            float3 QuantizeColor(float3 color, float steps)
            {
                return floor(color * steps + 0.5) / steps;
            }
            
            float BayerDither4x4(float2 screenPos)
            {
                float4x4 bayerMatrix = float4x4(
                    0.0/16.0,  8.0/16.0,  2.0/16.0, 10.0/16.0,
                    12.0/16.0, 4.0/16.0, 14.0/16.0,  6.0/16.0,
                    3.0/16.0, 11.0/16.0,  1.0/16.0,  9.0/16.0,
                    15.0/16.0, 7.0/16.0, 13.0/16.0,  5.0/16.0
                );
                
                int2 matrixPos = int2(screenPos.x % 4, screenPos.y % 4);
                return bayerMatrix[matrixPos.x][matrixPos.y];
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                float dither = BayerDither4x4(input.positionCS.xy);
                color.rgb += (dither - 0.5) * _DitherStrength * (1.0 / _ColorSteps);
                color.rgb = QuantizeColor(saturate(color.rgb), _ColorSteps);
                
                return color;
            }
            ENDHLSL
        }
        // Pass 1: Upsample with Sharpness Control
        Pass
        {
            Name "PixelArt Upsample"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize; // {1/w, 1/h, w, h}
            
            float _Sharpness;
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Smooth sample (Bilinear enabled in C# code)
                half4 smoothColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Sharp sample (Manual point filtering)
                float2 pixelUV = (floor(input.uv * _MainTex_TexelSize.zw) + 0.5) * _MainTex_TexelSize.xy;
                half4 sharpColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelUV);
                
                // Blend based on sharpness
                return lerp(smoothColor, sharpColor, _Sharpness);
            }
            ENDHLSL
        }
    }
}
