Shader "Custom/PixelPerfect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,1,1,1) // Overall outline color (not used in this case)
        _OutlineThickness ("Outline Thickness", Float) = 1.0
        _OutlineDepthMultiplier ("Outline Depth Multiplier", Float) = 1.0
        _OutlineDepthBias ("Outline Depth Bias", Float) = 0.0
        _OutlineNormalMultiplier ("Outline Normal Multiplier", Float) = 1.0
        _OutlineNormalBias ("Outline Normal Bias", Float) = 0.0
        _PixelationAmount ("Pixelation Amount", Float) = 1.0
        _DepthThreshold ("Depth Threshold", Float) = 0.0001
        _NormalsThreshold ("Normals Threshold", Float) = 0.2
        _NormalEdgeBias ("Normal Edge Bias", Vector) = (1,1,1)
        _DepthEdgeStrength ("Depth Edge Strength", Float) = 1.0
        _NormalEdgeStrength ("Normal Edge Strength", Float) = 0.5
        _LightBlendingStrength ("Light Blending Strength", Float) = 0.3 //Light blending for directional light

        //_DepthOutlineColor ("Depth Outline Color", Color) = (0, 0, 0, 1) // New property for depth outline color
        //_NormalOutlineColor ("Normal Outline Color", Color) = (1, 0, 0, 1) // New property for normal outline color
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            TEXTURE2D(_CameraNormalsTexture);
            SAMPLER(sampler_CameraNormalsTexture);

            TEXTURE2D(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);
            float4 _CameraColorTexture_TexelSize;

            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineDepthMultiplier;
            float _OutlineDepthBias;
            float _OutlineNormalMultiplier;
            float _OutlineNormalBias;
            float _PixelationAmount;
            float _DepthThreshold;
            float _NormalsThreshold;
            float3 _NormalEdgeBias;
            float _DepthEdgeStrength;
            float _NormalEdgeStrength;
            float _LightBlendingStrength;
            //float4 _DepthOutlineColor;   // Color for depth-based outline
            //float4 _NormalOutlineColor;  // Color for normal-based outline

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

            float GetDepth(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CameraDepthTexture, 
                    sampler_CameraDepthTexture, uv).r;
            }

            float3 GetNormal(float2 uv)
            {
                return SAMPLE_TEXTURE2D(_CameraNormalsTexture, 
                    sampler_CameraNormalsTexture, uv).rgb;
            }

            void Outline_float
            (float2 UV, float DepthThreshold, 
                float NormalsThreshold, 
                float3 NormalEdgeBias,             
                float DepthEdgeStrength, 
                float NormalEdgeStrength, 
                out float DepthOutline, 
                out float NormalOutline
            )
            {
                float2 texelSize = _CameraColorTexture_TexelSize.xy * 1;
                float depth = GetDepth(UV);
                float3 normal = GetNormal(UV);

                float2 uvs[4];
                uvs[0] = UV + float2(0.0, texelSize.y);   //Above
                uvs[1] = UV - float2(0.0, texelSize.y);   //Below
                uvs[2] = UV + float2(texelSize.x, 0.0);   //Right
                uvs[3] = UV - float2(texelSize.x, 0.0);   //Left

                //Depth edges
                float depths[4];
                float depthDifference = 0.0;

                for (int i = 0; i < 4; i++)
                {
                    depths[i] = GetDepth(uvs[i]);
                    depthDifference += depth - depths[i];
                }

                DepthOutline = step(DepthThreshold, depthDifference) * DepthEdgeStrength;

                //Normal edges
                float3 normals[4];
                float dotSum = 0.0;

                for (int j = 0; j < 4; j++)
                {
                    normals[j] = GetNormal(uvs[j]);
                    float3 normalDifference = normal - normals[j];

                    float normalBiasDiff = dot(normalDifference, NormalEdgeBias);
                    float normalIndicator = smoothstep(-.01, .01, normalBiasDiff);

                    //Only consider normal edges within a similar depth (avoids normals extending over the gameobject)
                    if (abs(depth - depths[j]) < DepthThreshold * 0.5)
                    {
                        dotSum += dot(normalDifference, normalDifference) * normalIndicator;
                    }

                    
                }

                float indicator = sqrt(dotSum);
                NormalOutline = step(NormalsThreshold, indicator) * NormalEdgeStrength;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 pixelatedUV = floor(input.uv * _PixelationAmount + 0.5) / _PixelationAmount; //Correct pixelation blur

                half4 sceneColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, pixelatedUV);

                float depthOutline, normalOutline;
                Outline_float(
                    pixelatedUV,
                    _DepthThreshold,       
                    _NormalsThreshold,     
                    _NormalEdgeBias,       
                    _DepthEdgeStrength,    
                    _NormalEdgeStrength,   
                    depthOutline,
                    normalOutline
                );

                
                float3 depthOutlineColor = sceneColor.rgb * 0.5;    //Darker outline for depth
                float3 normalOutlineColor = min(sceneColor.rgb * 1.5, 1.0); //Brighter outline for normal

                //Access main light color and direction
                Light mainLight = GetMainLight();
                float3 lightDir = mainLight.direction;
                float3 lightColor = mainLight.color;

                depthOutlineColor = lerp(depthOutlineColor, depthOutlineColor * lightColor, 0.5f);
                normalOutlineColor = lerp(normalOutlineColor, normalOutlineColor * lightColor, _LightBlendingStrength);

                //Blend each outline color with the scene color based on strength values
                float3 blendedDepthColor = lerp(sceneColor.rgb, depthOutlineColor, depthOutline * _DepthEdgeStrength);
                float3 blendedNormalColor = lerp(sceneColor.rgb, normalOutlineColor, normalOutline * _NormalEdgeStrength);

                //Combine both outlines with the original scene color
                float3 finalColor = blendedDepthColor + blendedNormalColor - sceneColor.rgb;
                
                return half4(finalColor, sceneColor.a);
            }

            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
