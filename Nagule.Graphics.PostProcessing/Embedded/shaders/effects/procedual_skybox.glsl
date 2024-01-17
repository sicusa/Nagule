#ifndef NAGULE_PROCEDUAL_SKYBOX
#define NAGULE_PROCEDUAL_SKYBOX

#include <nagule/common.glsl>

#define ATMOSPHERE_SAMPLES ProcedualSkybox_Samples
#define ATMOSPHERE_LIGHT_SAMPLES ProcedualSkybox_LightSamples
#define ATMOSPHERE_SUN_POWER ProcedualSkybox_SunPower
#define ATMOSPHERE_GROUND vec3(0)
#define ATMOSPHERE_STARS_LAYERS 3
#define ATMOSPHERE_STARS_ELEVATION ProcedualSkybox_StarsElevation * Time
#define ATMOSPHERE_STARS_AZIMUTH ProcedualSkybox_StarsAzimuth * Time

#ifndef ENVMAP_FNC
#define ENVMAP_FNC(NORM, ROUGHNESS, METALLIC) atmosphere(NORM, SunLightDirection)
#endif

#include <lygia/lighting/atmosphere.glsl>

vec3 ProcedualSkybox(vec3 color, float depth)
{
    return depth == 1
        ? color + atmosphere(normalize(EyeDirection), SunLightDirection) * ProcedualSkybox_Exposure
        : color;
}

#endif