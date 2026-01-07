#ifndef LIGHTING_CEL_SHADED_INCLUDED
#define LIGHTING_CEL_SHADED_INCLUDED

#ifndef SHADERGRAPH_PREVIEW

struct EdgeConstants{

    float diffuse;
    float specular;
    float specularOffset;
    float fresnel;
    float fresnelOffset;
    float distanceAttenuation;
    float shadowAttenuation;
};
struct SurfaceVariables {
    float3 normal;
    float3 view;
    float smoothness;
    float bright;
    float fresnelThreshold;
    EdgeConstants ec;
};

float3 CalculateCelShading(Light l, SurfaceVariables s) {

    float shadowAttenuationSmooth = smoothstep(0.0, s.ec.shadowAttenuation, l.shadowAttenuation);
    float distanceAttenuationSmooth = smoothstep(0.0, s.ec.distanceAttenuation, l.distanceAttenuation);

    //float attenuation = l.shadowAttenuation * l.distanceAttenuation; //shadow atten between 0-1 (OLD, WITHOUT SMOOTHSTEP)
    float attenuation = shadowAttenuationSmooth * distanceAttenuationSmooth;
    float diffuse = saturate(dot(s.normal, l.direction)); // diffuse clamps between 0-1
    diffuse *= attenuation;

    float3 h = SafeNormalize(l.direction + s.view); //halfway vector
    float specular = saturate(dot(s.normal, h));
    specular = pow(specular, s.bright);
    specular *= diffuse * s.smoothness; //Avoids specular appearing at wrong positions

    float fresnel = 1 - dot(s.view, s.normal);
    fresnel *= pow(diffuse, s.fresnelThreshold);

    diffuse = smoothstep(0.0, s.ec.diffuse, diffuse);
    specular = s.smoothness * smoothstep((1-s.smoothness) * s.ec.specular + s.ec.specularOffset, s.ec.specular + s.ec.specularOffset, specular);
    fresnel = s.smoothness * smoothstep(s.ec.fresnel - 0.5 * s.ec.fresnelOffset, s.ec.fresnel + 0.5 * s.ec.fresnelOffset, fresnel);

    return l.color * (diffuse + max(specular, fresnel)); 
}
#endif

void LightingCelShaded_float(float Smoothness, float FresnelThreshold, float3 WorldPos, float3 Normal, float3 View, float EdgeDiffuse,
    float EdgeSpecular, float EdgeSpecularOffset, float EdgeDistanceAttenuation, float EdgeShadowAttenuation, float EdgeFresnel,
    float EdgeFresnelOffset, out float3 Color) {

    #if defined(SHADERGRAPH_PREVIEW)
        Color = half3(0.5, 0.5, 0.5);
    #else //Initialize surface s variables
        SurfaceVariables s;
        s.normal = normalize(Normal);
        s.view = SafeNormalize(View);
        s.smoothness = Smoothness;
        s.bright = exp2(10 * Smoothness + 1);
        s.fresnelThreshold = FresnelThreshold;
        s.ec.diffuse = EdgeDiffuse;
        s.ec.specular = EdgeSpecular;
        s.ec.specularOffset = EdgeSpecularOffset;
        s.ec.distanceAttenuation = EdgeDistanceAttenuation;
        s.ec.shadowAttenuation = EdgeShadowAttenuation;
        s.ec.fresnel = EdgeFresnel;
        s.ec.fresnelOffset = EdgeFresnelOffset;

        #if SHADOWS_SCREEN
            half4 clipPos = TransformWorldToHClip(WorldPos);
            half4 shadowCoord = ComputeScreenPos(clipPos);
        #else
            half4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
        #endif

        Light light = GetMainLight(shadowCoord); 
        Color = CalculateCelShading(light, s);

        //Additional Lights
        int pixelLightCount = GetAdditionalLightsCount();
        for(int i = 0; i < pixelLightCount; i++){
            light = GetAdditionalLight(i, WorldPos, 1);
            Color += CalculateCelShading(light, s);
        }
    #endif
}

#endif
