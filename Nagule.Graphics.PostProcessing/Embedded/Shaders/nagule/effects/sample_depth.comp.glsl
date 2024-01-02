#ifndef SAMPLE_DEPTH
#define SAMPLE_DEPTH

#include <nagule/common.glsl>

vec3 SampleDepth(vec3 color)
{
    return vec3(LinearizeDepth(texture(DepthTex, TexCoord).r));
}

#endif