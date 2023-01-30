#ifndef PARALLAX_MAPPING
#define PARALLAX_MAPPING

vec2 ParallaxOcclusionMapping(sampler2D heightTex, vec2 texCoord, vec3 viewDir)
{
    const float minLayers = 8.0;
    const float maxLayers = 32.0;

    float numLayers = mix(maxLayers, minLayers, abs(dot(vec3(0.0, 0.0, 1.0), viewDir)));
    float layerDepth = 1.0 / numLayers;
    float currentLayerDepth = 0.0;
    vec2 P = viewDir.xy * HeightScale;
    vec2 deltaTexCoord = P / numLayers;

    vec2 currentTexCoord = texCoord;
    float height = texture(heightTex, currentTexCoord).r;

    while (currentLayerDepth < height) {
        currentTexCoord -= deltaTexCoord;
        height = texture(heightTex, currentTexCoord).r;
        currentLayerDepth += layerDepth;
    }

    vec2 prevTexCoord = currentTexCoord + deltaTexCoord;

    float afterDepth = currentDepthMapValue - currentLayerDepth;
    float beforeDepth = texture(heightTex, prevTexCoord).r - currentLayerDepth + layerDepth;

    float weight = afterDepth / (afterDepth - beforeDepth);
    vec2 finalTexCoord = prevTexCoord * weight + currentTexCoord * (1.0 - weight);

    return finalTexCoord;
}

float ParallaxSoftShadowMultiplier(vec3 lightDir, vec2 texCoord, float height)
{
    const float minLayers = 15.0;
    const float maxLayers = 30.0;

    if (dot(vec3(0, 0, 1), lightDir) <= 0) {
        return 0;
    }

    float numLayers = mix(maxLayers, minLayers, abs(dot(vec3(0, 0, 1), lightDir)));
    float height = texture(HeightTex, texCoord).r;
    float layerHeight = height / numLayers;
    vec2 texStep = HeightScale * lightDir.xy / lightDir.z / numLayers;

    float currentLayerHeight = height - layerHeight;
    vec2 currentTexCoord = texCoord + texStep;
    float depthFromTexture = texture(depthMap, currentTexCoord).r;
    int stepIndex = 1;

    float numSamplesUnderSurface = 0;
    float shadowMultiplier = 0;

    while (currentLayerHeight > 0.0) { 
        if (depthFromTexture < currentLayerHeight) {
            numSamplesUnderSurface += 1;
            float newShadowMultiplier = (currentLayerHeight - depthFromTexture) * (1.0 - stepIndex / numLayers);
            shadowMultiplier = max(shadowMultiplier, newShadowMultiplier);
        }

        stepIndex += 1;
        currentLayerHeight -= layerHeight;
        currentTexCoord += texStep;
        depthFromTexture = texture(depthMap, currentTexCoord).r;
    }

    return numSamplesUnderSurface < 1 ? 0 : shadowMultiplier;
}

#endif