#ifndef AECO_TRANSPARENCY
#define AECO_TRANSPARENCY

float GetTransparencyWeight(float z, float a) {
    return a * max(0.01, min(3e3, 10 / (1e-5 + z * z * 0.25 + pow(z / 200, 6))));
}

float GetTransparency(float a) {
    return GetTransparencyWeight(gl_FragCoord.z, a) * a;
}

#endif