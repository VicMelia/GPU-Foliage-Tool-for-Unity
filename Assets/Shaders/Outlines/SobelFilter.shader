Shader "Custom/SobelFilter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineThickness ("Outline Thickness", Float) = 1.0
        _OutlineDepthMultiplier ("Outline Depth Multiplier", Float) = 1.0
        _OutlineDepthBias ("Outline Depth Bias", Float) = 0.0
        _OutlineNormalMultiplier ("Outline Normal Multiplier", Float) = 1.0
        _OutlineNormalBias ("Outline Normal Bias", Float) = 0.0
        _PixelationAmount ("Pixelation Amount", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineDepthMultiplier;
            float _OutlineDepthBias;
            float _OutlineNormalMultiplier;
            float _OutlineNormalBias;
            float _PixelationAmount;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                return output;
            }

            float4 SobelSample(Texture2D t, SamplerState s, float2 uv, float3 offset)
            {
                float4 pixelCenter = t.Sample(s, uv);
                float4 pixelLeft   = t.Sample(s, uv - offset.xz);
                float4 pixelRight  = t.Sample(s, uv + offset.xz);
                float4 pixelUp     = t.Sample(s, uv + offset.zy);
                float4 pixelDown   = t.Sample(s, uv - offset.zy);
                
                return abs(pixelLeft  - pixelCenter) +
                       abs(pixelRight - pixelCenter) +
                       abs(pixelUp    - pixelCenter) +
                       abs(pixelDown  - pixelCenter);
            }

            half4 frag(Varyings input) : SV_Target
            {

                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Texel size affects outline width
                float2 texelSize = _OutlineThickness / _ScreenParams.xy; 

                // Depth of the current pixel
                float depthCenter = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv).r;

                // Sample depth values from neighboring pixels
                float depthTL = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(-texelSize.x, texelSize.y)).r;
                float depthT = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(0, texelSize.y)).r;
                float depthTR = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(texelSize.x, texelSize.y)).r;
                float depthL = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(-texelSize.x, 0)).r;
                float depthR = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(texelSize.x, 0)).r;
                float depthBL = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(-texelSize.x, -texelSize.y)).r;
                float depthB = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(0, -texelSize.y)).r;
                float depthBR = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, input.uv + float2(texelSize.x, -texelSize.y)).r;

                // Sobel filter kernel for depth
                float edgeXDepth = (depthTR + 2 * depthR + depthBR - depthTL - 2 * depthL - depthBL);
                float edgeYDepth = (depthBL + 2 * depthB + depthBR - depthTL - 2 * depthT - depthTR);
                float edgeMagnitudeDepth = sqrt(edgeXDepth * edgeXDepth + edgeYDepth * edgeYDepth);

                // Calculate sobel depth with multiplier and bias
                float sobelDepth = pow(saturate(edgeMagnitudeDepth) * _OutlineDepthMultiplier, _OutlineDepthBias);

                // Sample the normal buffer and build a composite scalar value
                float3 offset = float3((1.0 / _ScreenParams.x), (1.0 / _ScreenParams.y), 0.0) * _OutlineThickness;
                float3 sobelNormalVec = SobelSample(_CameraNormalsTexture, sampler_CameraNormalsTexture, input.uv, offset).rgb;
                float sobelNormal = sobelNormalVec.x + sobelNormalVec.y + sobelNormalVec.z;

                // Normalize sobelNormal based on a threshold
                float normalizedNormal = saturate(sobelNormal / 3.0); // Assuming the range of sobelNormal is [0, 3]

                // Calculate sobel normal with multiplier and bias
                float sobelNormalIntensity = pow(normalizedNormal * _OutlineNormalMultiplier, _OutlineNormalBias);

                // Lerp between scene color and outline color based on sobel depth and sobel normal intensity
                float3 outlineColor = lerp(sceneColor, _OutlineColor.rgb, _OutlineColor.a);
                float3 finalColor = lerp(sceneColor.rgb, outlineColor, max(sobelDepth, sobelNormalIntensity));

                return half4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
