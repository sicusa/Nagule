#ifndef AECO_BLINN_PHONG
#define AECO_BLINN_PHONG

#include <nagule/common.glsl>
#include <nagule/lighting.glsl>

uniform sampler2D DiffuseTex;
uniform sampler2D SpecularTex;
uniform sampler2D EmissionTex;

struct LightingResult {
    vec3 Diffuse;
    vec3 Specular;
};

vec4 CalculateBlinnPhongLighting(vec3 position, vec2 texCoord, vec3 normal, float depth)
{
    vec2 tiledCoord = texCoord * Tiling;
    vec4 diffuseColor = Diffuse * texture(DiffuseTex, tiledCoord);
    vec4 specularColor = Specular * texture(SpecularTex, tiledCoord);

    vec3 diffuse = vec3(0);
    vec3 specular = vec3(0);

    for (int i = 0; i < GlobalLightCount; i++) {
        Light light = FetchGlobalLight(GlobalLightIndeces[i]);
        int category = light.Category;
        vec3 lightColor = light.Color.rgb * light.Color.a;

        if (category == LIGHT_AMBIENT) {
            diffuse += lightColor;
        }
        else if (category == LIGHT_DIRECTIONAL) {
            vec3 lightDir = -light.Direction;
            float diff = max(0.8 * dot(normal, lightDir) + 0.2, 0.0);
            diffuse += diff * lightColor;

            vec3 viewDir = normalize(CameraPosition - position);
            vec3 divisor = normalize(viewDir + lightDir);
            float spec = pow(max(dot(divisor, normal), 0.0), Shininess);
            specular += spec * lightColor;
        }
    }

    int clusterIndex = GetClusterIndex(gl_FragCoord.xy, depth);
    int lightCount = FetchLightCount(clusterIndex);

    for (int i = 0; i < lightCount; i++) {
        Light light = FetchLightFromCluster(clusterIndex, i);
        int category = light.Category;
        vec3 lightColor = light.Color.rgb * light.Color.a;

        vec3 lightDir = light.Position - position;
        float distance = length(lightDir);
        lightDir /= distance;

        vec3 viewDir = normalize(CameraPosition - position);
        vec3 divisor = normalize(viewDir + lightDir);
        float diff = max(0.8 * dot(normal, lightDir) + 0.2, 0.0);
        float spec = pow(max(dot(divisor, normal), 0.0), Shininess);

        if (category == LIGHT_SPOT) {
            vec2 coneCutoffs = light.ConeCutoffsOrAreaSize;

            float theta = dot(lightDir, normalize(-light.Direction));
            float epsilon = coneCutoffs.x - coneCutoffs.y;
            float intensity = clamp((theta - coneCutoffs.y) / epsilon, 0.0, 1.0);

            diff *= intensity;
            spec *= intensity;
        }

        float attenuation = CalculateLightAttenuation(light, distance);
        diffuse += diff * attenuation * lightColor;
        specular += spec * attenuation * lightColor;
    }

    vec4 emissionColor = Emission * texture(EmissionTex, tiledCoord);
    vec3 emission = emissionColor.rgb * emissionColor.a;

    return vec4(diffuse * diffuseColor.rgb + specular * specularColor.rgb + emission, diffuseColor.a);
}

#endif