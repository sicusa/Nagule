#ifndef NAGULE_SHADOW_MAPPING
#define NAGULE_SHADOW_MAPPING

#define NAGULE_MAXIMUM_SHADOW_SAMPLER_COUNT 127
#define NAGULE_SHADOW_SAMPLER_CELLAR_COUNT 109

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
    ShadowMapSamplerSlot ShadowMapSamplerHashMap[NAGULE_MAXIMUM_SHADOW_SAMPLER_COUNT];
};

bool FindShadowMapSampler(int lightIndex, out ShadowMapSampler shadowMapSampler)
{
    int slotIndex = lightIndex % NAGULE_SHADOW_SAMPLER_CELLAR_COUNT;

    while (true) {
        int slotLightIndex = ShadowMapSamplerHashMap[slotIndex].LightIndex;
        if (slotLightIndex == lightIndex) {
            shadowMapSampler = ShadowMapSamplerHashMap[slotIndex].Sampler;
            return true;
        }
        slotIndex = ShadowMapSamplerHashMap[slotIndex].NextSlotIndex;
        if (slotIndex == -1) {
            return false;
        }
    }
}

#endif