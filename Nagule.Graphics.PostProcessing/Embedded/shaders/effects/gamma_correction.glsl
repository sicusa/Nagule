#ifndef GAMMA_CORRECTION
#define GAMMA_CORRECTION

vec3 GammaCorrection(vec3 color, float depth)
{
    return pow(color, vec3(1.0 / GammaCorrection_Gamma));
}

#endif