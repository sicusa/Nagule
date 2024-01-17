#ifndef NAGULE_CUBEMAP_SKYBOX
#define NAGULE_CUBEMAP_SKYBOX

#include <nagule/common.glsl>

vec3 CubemapSkybox(vec3 color, float depth)
{
    vec3 tex = vec3(-EyeDirection.x, -EyeDirection.y, EyeDirection.z);
    return depth == 1
        ? color + texture(CubemapSkybox_Cubemap, tex).rgb
        : color;
}

#endif