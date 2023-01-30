#ifndef NAGULE_NOISE
#define NAGULE_NOISE

#include <nagule/common.glsl>

float Rand(vec2 co) { return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453); }
float Rand(vec2 co, float l) { return Rand(vec2(Rand(co), l));}
float Rand(vec2 co, float l, float t) { return Rand(vec2(Rand(co, l), t)); }

float PerlinNoise(vec2 p, float dim, float time)
{
    vec2 pos = floor(p * dim);
    vec2 posx = pos + vec2(1.0, 0.0);
    vec2 posy = pos + vec2(0.0, 1.0);
    vec2 posxy = pos + vec2(1.0);

    float c = Rand(pos, dim, time);
    float cx = Rand(posx, dim, time);
    float cy = Rand(posy, dim, time);
    float cxy = Rand(posxy, dim, time);

    vec2 d = fract(p * dim);
    d = -0.5 * cos(d * PI) + 0.5;

    float ccx = mix(c, cx, d.x);
    float cycxy = mix(cy, cxy, d.x);
    float center = mix(ccx, cycxy, d.y);

    return center * 2.0 - 1.0;
}

float PerlinNoise(vec2 p, float dim) {
    return PerlinNoise(p, dim, 0.0);
}

#endif