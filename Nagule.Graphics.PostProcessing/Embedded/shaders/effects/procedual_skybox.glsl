#ifndef PROCEDUAL_SKYBOX
#define PROCEDUAL_SKYBOX

#include <nagule/common.glsl>

#define ATMOSPHERE_LIGHT_SAMPLES ProcedualSkybox_LightSamples
#define ATMOSPHERE_SAMPLES ProcedualSkybox_Samples
#define ATMOSPHERE_GROUND ProcedualSkybox_Ground
#define ATMOSPHERE_STARS_LAYERS 3
#define ATMOSPHERE_STARS_ELEVATION ProcedualSkybox_StarsElevation * Time
#define ATMOSPHERE_STARS_AZIMUTH ProcedualSkybox_StarsAzimuth * Time

#include <lygia/lighting/atmosphere.glsl>

vec3 ProcedualSkybox(vec3 color, float depth)
{
    return depth == 1
        ? color + atmosphere(EyeDirection, vec3(0))
        : color;
}

#endif