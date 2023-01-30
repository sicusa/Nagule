#ifndef NAGULE_PARALLAX_MAPPING
#define NAGULE_PARALLAX_MAPPING

vec2 ParallaxOcclusionMapping(sampler2D heightTex, vec2 texCoord, vec3 viewDir, float heightScale)
{ 
    const float minLayers = 8;
    const float maxLayers = 32;

    float numLayers = mix(maxLayers, minLayers, abs(dot(vec3(0.0, 0.0, 1.0), viewDir)));  
    float layerDepth = 1.0 / numLayers;
    float currentLayerDepth = 0.0;
    vec2 P = viewDir.xy / viewDir.z * heightScale; 
    vec2 deltaTexCoords = P / numLayers;
  
    vec2 currentTexCoords = texCoord;
    float currentDepthMapValue = 1 - texture(heightTex, currentTexCoords).r;
      
    while (currentLayerDepth < currentDepthMapValue) {
        currentTexCoords -= deltaTexCoords;
        currentDepthMapValue = 1 - texture(heightTex, currentTexCoords).r;  
        currentLayerDepth += layerDepth;  
    }
    
    vec2 prevTexCoords = currentTexCoords + deltaTexCoords;

    float afterDepth  = currentDepthMapValue - currentLayerDepth;
    float beforeDepth = 1 - texture(heightTex, prevTexCoords).r - currentLayerDepth + layerDepth;
 
    float weight = afterDepth / (afterDepth - beforeDepth);
    return prevTexCoords * weight + currentTexCoords * (1.0 - weight);
}

float ParallaxSoftShadowMultiplier(sampler2D heightTex, vec2 texCoord, vec3 lightDir, float heightScale)
{
    const float minLayers = 15.0;
    const float maxLayers = 30.0;

    if (dot(vec3(0, 0, 1), lightDir) <= 0) {
        return 0;
    }

    float numLayers = mix(maxLayers, minLayers, abs(dot(vec3(0, 0, 1), lightDir)));
    float height = texture(heightTex, texCoord).r;
    float layerHeight = height / numLayers;
    vec2 texStep = heightScale * lightDir.xy / lightDir.z / numLayers;

    float currentLayerHeight = height - layerHeight;
    vec2 currentTexCoord = texCoord + texStep;
    float depthFromTexture = texture(heightTex, currentTexCoord).r;
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
        depthFromTexture = texture(heightTex, currentTexCoord).r;
    }

    return numSamplesUnderSurface < 1 ? 0 : shadowMultiplier;
}

#endif