#ifndef NAGULE_FXAA
#define NAGULE_FXAA

#include <nagule/common.glsl>

#include <lygia/sample/fxaa.glsl>

vec3 FXAA(vec3 color, float depth)
{
    vec2 pixel = 1 / vec2(ViewportWidth, ViewportHeight);
    return sampleFXAA(ColorTex, TexCoord, pixel).rgb;
}

#endif