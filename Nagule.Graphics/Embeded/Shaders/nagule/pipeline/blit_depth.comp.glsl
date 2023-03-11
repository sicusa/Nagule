#ifndef BLIT_DEPTH
#define BLIT_DEPTH

#include <nagule/common.glsl>

vec3 BlitDepth(vec3 color)
{
    return vec3(LinearizeDepth(texture(DepthTex, TexCoord).r));
}

#endif