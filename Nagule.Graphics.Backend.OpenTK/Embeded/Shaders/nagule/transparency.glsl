#ifndef NAGULE_TRANSPARENCY
#define NAGULE_TRANSPARENCY

float GetTransparencyWeight(float z, vec4 color) {
    return max(min(1.0, max(max(color.r, color.g), color.b) * color.a), color.a) *
      clamp(0.03 / (1e-5 + pow(z / 200, 4.0)), 1e-2, 3e3);
}

float GetTransparencyWeight(vec4 color) {
    return GetTransparencyWeight(gl_FragCoord.z, color);
}

#endif