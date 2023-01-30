#ifndef NAGULE_PARALLAX_MAPPING
#define NAGULE_PARALLAX_MAPPING

vec2 ParallaxOcclusionMapping(sampler2D heightTex, vec2 texCoord, vec3 viewDir, float heightScale)
{ 
    const float minLayers = 8;
    const float maxLayers = 32;

    vec2 dx = dFdx(texCoord);
    vec2 dy = dFdy(texCoord);

    float layerCount = mix(maxLayers, minLayers, abs(dot(vec3(0.0, 0.0, 1.0), viewDir)));  
    float layerDepth = 1.0 / layerCount;
    float currLayerDepth = 0.0;
    vec2 deltaTexCoords = viewDir.xy / viewDir.z * heightScale / layerCount;
  
    vec2 currTexCoords = texCoord;
    float currDepthMapValue = 1 - textureGrad(heightTex, currTexCoords, dx, dy).r;
      
    while (currLayerDepth < currDepthMapValue) {
        currTexCoords -= deltaTexCoords;
        currLayerDepth += layerDepth;  
        currDepthMapValue = 1 - textureGrad(heightTex, currTexCoords, dx, dy).r;  
    }
    
    vec2 prevTexCoords = currTexCoords + deltaTexCoords;
    float afterDepth  = currDepthMapValue - currLayerDepth;
    float beforeDepth = 1 - textureGrad(heightTex, prevTexCoords, dx, dy).r - currLayerDepth + layerDepth;
 
    float weight = afterDepth / (afterDepth - beforeDepth);
    return prevTexCoords * weight + currTexCoords * (1.0 - weight);
}

float ParallaxSoftShadowMultiplier(sampler2D heightTex, vec2 texCoord, vec3 lightDir, float heightScale)
{
    const float minLayers = 15.0;
    const float maxLayers = 30.0;

    if (dot(vec3(0, 0, 1), lightDir) <= 0) {
        return 0;
    }

    float layerCount = mix(maxLayers, minLayers, abs(dot(vec3(0, 0, 1), lightDir)));
    float height = texture(heightTex, texCoord).r;
    float layerHeight = height / layerCount;
    vec2 texStep = heightScale * lightDir.xy / lightDir.z / layerCount;

    float currentLayerHeight = height - layerHeight;
    vec2 currentTexCoord = texCoord + texStep;
    float depthFromTexture = texture(heightTex, currentTexCoord).r;
    int stepIndex = 1;

    float numSamplesUnderSurface = 0;
    float shadowMultiplier = 0;

    while (currentLayerHeight > 0.0) { 
        if (depthFromTexture < currentLayerHeight) {
            numSamplesUnderSurface += 1;
            float newShadowMultiplier = (currentLayerHeight - depthFromTexture) * (1.0 - stepIndex / layerCount);
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