#ifndef NAGULE_PARALLAX_MAPPING
#define NAGULE_PARALLAX_MAPPING

vec2 ParallaxOcclusionMapping(
    sampler2D heightTex, vec2 texCoord, vec3 viewDir,
    float heightScale, int minLayerCount, int maxLayerCount)
{ 
    vec2 dx = dFdx(texCoord);
    vec2 dy = dFdy(texCoord);

    float layerCount = mix(maxLayerCount, minLayerCount, abs(viewDir.z));  
    float layerDepth = 1.0 / layerCount;
    vec2 deltaTexCoord = viewDir.xy / viewDir.z * heightScale / layerCount;
  
    vec2 currTexCoords = texCoord;
    float currLayerDepth = 0.0;
    float currDepthMapValue = 1 - texture(heightTex, texCoord).r;
      
    while (currLayerDepth < currDepthMapValue) {
        currTexCoords -= deltaTexCoord;
        currLayerDepth += layerDepth;  
        currDepthMapValue = 1 - textureGrad(heightTex, currTexCoords, dx, dy).r;  
    }
    
    vec2 prevTexCoords = currTexCoords + deltaTexCoord;
    float afterDepth  = currDepthMapValue - currLayerDepth;
    float beforeDepth = 1 - textureGrad(heightTex, prevTexCoords, dx, dy).r - currLayerDepth + layerDepth;
 
    float weight = afterDepth / (afterDepth - beforeDepth);
    return prevTexCoords * weight + currTexCoords * (1.0 - weight);
}

float ParallaxSoftShadowMultiplier(
    sampler2D heightTex, vec2 texCoord, vec3 lightDir,
    float heightScale, int minLayerCount, int maxLayerCount)
{
    if (lightDir.z < 0) {
        return 1;
    }

    float sampleCounter = 0;
    float initialHeight = 1 - texture(heightTex, texCoord).r;
    float layerCount = mix(maxLayerCount, minLayerCount, abs(lightDir.z));
    float layerDepth = initialHeight / layerCount;
    vec2 deltaTexCoord = lightDir.xy / lightDir.z * heightScale / layerCount;

    vec2 currTexCoord = texCoord + deltaTexCoord;
    float currLayerDepth = initialHeight - layerDepth;
    float currDepthMapValue = 1 - texture(heightTex, currTexCoord).r;

    float shadowMultiplier = 0;
    int stepIndex = 1;

    while (currLayerDepth > 0) {
        if (currDepthMapValue < currLayerDepth) {
            sampleCounter += 1;
            float newShadowMultiplier =
                (currLayerDepth - currDepthMapValue) * (1.0 - stepIndex / layerCount);
            shadowMultiplier = max(shadowMultiplier, newShadowMultiplier);
        }
        stepIndex += 1;
        currLayerDepth -= layerDepth;
        currTexCoord += deltaTexCoord;
        currDepthMapValue = 1 - texture(heightTex, currTexCoord).r;
    }

    return sampleCounter < 1 ? 1 : 1.0 - shadowMultiplier;
}

#endif