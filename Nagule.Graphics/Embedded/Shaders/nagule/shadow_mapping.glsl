#ifndef NAGULE_SHADOW_MAPPING
#define NAGULE_SHADOW_MAPPING

layout(std140) uniform ShadowMapSettings
{
    int ShadowMapWidth;
    int ShadowMapHeight;
};

uniform sampler2DArray ShadowMapTileset;

#endif