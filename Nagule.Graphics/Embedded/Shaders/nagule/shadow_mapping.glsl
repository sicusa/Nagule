#ifndef NAGULE_SHADOW_MAPPING
#define NAGULE_SHADOW_MAPPING

#define SHADOW_MAP_SAMPLER_HASH_MAP_CAPACITY 127

struct ShadowMapSampler
{
    int ShadowMapIndex;
    float Strength;
};

struct ShadowMapSamplerSlot
{
    int LightIndex;
    int NextSlotIndex;
    ShadowMapSampler Sampler;
};

layout(std140) uniform ShadowMapLibrary
{
    int ShadowMapWidth;
    int ShadowMapHeight;

    // primary & secondary light must be directional
    int PrimaryLightShadowMapIndex;
    int SecondaryLightShadowMapIndex;

    mat4 PrimaryLightMatrix;
    mat4 SecondaryLightMatrix;

    // last slot of the hash map will always be empty
    ShadowMapSamplerSlot ShadowMapSamplerHashMap[SHADOW_MAP_SAMPLER_HASH_MAP_CAPACITY + 1];
};

bool FindShadowMapSampler(int lightIndex, out ShadowMapSampler shadowMapSampler)
{
    int slotIndex = lightIndex % SHADOW_MAP_SAMPLER_HASH_MAP_CAPACITY;

    while (true) {
        int slotLightIndex = ShadowMapSamplerHashMap[slotIndex].LightIndex;
        if (slotLightIndex == -1) {
            return false;
        }
        if (slotLightIndex == lightIndex) {
            break;
        }
        slotIndex = ShadowMapSamplerHashMap[slotIndex].NextSlotIndex;
    }

    shadowMapSampler = ShadowMapSamplerHashMap[slotIndex].Sampler;
    return true;
}

#endif