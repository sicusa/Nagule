#version 410 core

#include <nagule/common.glsl>
#include <nagule/lighting.glsl>

#ifdef _HeightTex
#include <nagule/parallax_mapping.glsl>
#endif

properties
{
    vec4 Diffuse = vec4(1);
    vec4 Specular = vec4(0);
    vec4 Ambient = vec4(0);
    vec4 Emission = vec4(0);
    float Shininess = 0;
    float Reflectivity = 0;
    vec2 Tiling = vec2(1);
    vec2 Offset = vec2(0);
    float Threshold = 0.9;
    float HeightScale = 1;
}

uniform sampler2D DiffuseTex;
uniform sampler2D SpecularTex;
uniform sampler2D NormalTex;
uniform sampler2D HeightTex;
uniform sampler2D EmissionTex;

in VertexOutput {
    vec2 TexCoord;

#ifndef LightingMode_Unlit
    vec3 Position;
#if defined(_NormalTex) || defined(_HeightTex)
    mat3 TBN;
#else
    vec3 Normal;
#endif
#endif

#if defined(LightingMode_Full) || defined(LightingMode_Local)
    float Depth;
#endif
} i;

#ifdef RenderMode_Transparent
#include <nagule/transparency.glsl>
OUT_ACCUM vec4 Accum;
OUT_REVEAL float Reveal;
#else
out vec4 FragColor;
#endif

void main()
{
    vec2 tiledCoord = i.TexCoord;
#ifdef _Tiling
    tiledCoord *= Tiling;
#endif
#ifdef _Offset
    tiledCoord += Offset;
#endif

#if (!defined(LightingMode_Unlit) && defined(_Specular)) || defined(_HeightTex)
    vec3 viewDir = normalize(CameraPosition - i.Position);
#endif

#ifdef _HeightTex
    tiledCoord = ParallaxOcclusionMapping(HeightTex, tiledCoord, viewDir * i.TBN, HeightScale);
    if (tiledCoord.x > 1.0 || tiledCoord.y > 1.0 || tiledCoord.x < 0.0 || tiledCoord.y < 0.0) {
        discard;
    }
#endif

#ifdef _DiffuseTex
    vec4 diffuseColor = Diffuse * texture(DiffuseTex, tiledCoord);
#else
    vec4 diffuseColor = Diffuse;
#endif

#ifdef RenderMode_Cutoff
    if (color.a < Threshold) {
        discard;
    }
#endif

#ifndef LightingMode_Unlit
    vec3 diffuse = vec3(0);
    vec3 position = i.Position;
#if defined(_NormalTex)
    vec3 normal = texture(NormalTex, tiledCoord).rgb;
    normal = normal * 2.0 - vec3(1.0);
    normal = normalize(i.TBN * normal);
#elif defined(_HeightTex)
    vec3 normal = i.TBN[2];
#else
    vec3 normal = i.Normal;
#endif
#endif

#if !defined(LightingMode_Unlit) && defined(_Specular)
    vec3 specular = vec3(0);
#ifdef _SpecularTex
    vec4 specularColor = Specular * texture(SpecularTex, tiledCoord);
#else
    vec4 specularColor = Specular;
#endif
#endif

#if defined(LightingMode_Full) || defined(LightingMode_Global)
    for (int i = 0; i < GlobalLightCount; i++) {
        Light light = FetchGlobalLight(GlobalLightIndices[i]);
        int category = light.Category;
        vec3 lightColor = light.Color.rgb * light.Color.a;

        if (category == LIGHT_DIRECTIONAL) {
            vec3 lightDir = -light.Direction;
            float diff = max(0.8 * dot(normal, lightDir) + 0.2, 0.0);
            diffuse += diff * lightColor;

        #ifdef _Specular
            vec3 divisor = normalize(viewDir + lightDir);
            float spec = pow(max(dot(divisor, normal), 0.0), Shininess);
            specular += spec * lightColor;
        #endif
        }
        else if (category == LIGHT_AMBIENT) {
            diffuse += lightColor;
        }
    }
#endif

#if defined(LightingMode_Full) || defined(LightingMode_Local)
    int clusterIndex = GetClusterIndex(gl_FragCoord.xy, i.Depth);
    int lightCount = FetchLightCount(clusterIndex);

    for (int i = 0; i < lightCount; i++) {
        Light light = FetchLightFromCluster(clusterIndex, i);
        int category = light.Category;

        vec3 lightDir = light.Position - position;
        float distance = length(lightDir);
        lightDir /= distance;

        float diff = max(dot(normal, lightDir), 0.0);
    #ifdef _Specular
        vec3 divisor = normalize(viewDir + lightDir);
        float spec = pow(max(dot(divisor, normal), 0.0), Shininess);
    #endif

        if (category == LIGHT_SPOT) {
            vec2 coneCutoffs = light.ConeCutoffsOrAreaSize;

            float theta = dot(lightDir, -light.Direction);
            float epsilon = coneCutoffs.x - coneCutoffs.y;
            float intensity = clamp((theta - coneCutoffs.y) / epsilon, 0.0, 1.0);

            diff *= intensity;
        #ifdef _Specular
            spec *= intensity;
        #endif
        }

        vec3 lightColor = light.Color.rgb * light.Color.a;
        float attenuation = CalculateLightAttenuation(light.Range, distance);

        diffuse += diff * attenuation * lightColor;
    #ifdef _Specular
        specular += spec * attenuation * lightColor;
    #endif
    }
#endif

#ifndef LightingMode_Unlit
    vec3 color = diffuse * diffuseColor.rgb;
#else
    vec3 color = diffuseColor.rgb;
#endif

#if !defined(LightingMode_Unlit) && defined(_Specular)
    color = color + specular * specularColor.rgb;
#endif

#ifdef _Emission
#ifdef _EmissionTex
    vec4 emissionColor = Emission * texture(EmissionTex, tiledCoord);
    color = color + emissionColor.a * emissionColor.rgb;
#else
    color = color + Emission.a * Emission.rgb;
#endif
#endif

#ifdef RenderMode_Transparent
    float alpha = diffuseColor.a;
    Reveal = GetTransparencyWeight(vec4(color, alpha)) * alpha;
    Accum = vec4(color * Reveal, alpha);
#else
    FragColor = vec4(color, diffuseColor.a);
#endif
}