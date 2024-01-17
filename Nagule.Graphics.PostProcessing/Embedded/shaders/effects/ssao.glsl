#ifndef NAGULE_SSAO
#define NAGULE_SSAO

#include <nagule/common.glsl>

#define SSAO_SAMPLES_NUM SSAO_Samples
#define CAMERA_NEAR_CLIP CameraNearPlaneDistance
#define CAMERA_FAR_CLIP CameraFarPlaneDistance

#include <lygia/space/ratio.glsl>
#include <lygia/lighting/ssao.glsl>

vec3 SSAO(vec3 color, float depth)
{
    vec2 resolution = vec2(ViewportWidth, ViewportHeight);
    return color * ssao(DepthTex, TexCoord, 1.0 / resolution, SSAO_Radius);
}

#endif