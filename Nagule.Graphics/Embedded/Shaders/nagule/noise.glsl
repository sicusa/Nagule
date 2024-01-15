#ifndef NAGULE_NOISE
#define NAGULE_NOISE

#include <nagule/common.glsl>
#include <nagule/random.glsl>

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

float Mod289(float x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 Mod289(vec4 x){return x - floor(x * (1.0 / 289.0)) * 289.0;}
vec4 Perm(vec4 x){return Mod289(((x * 34.0) + 1.0) * x);}

float noise(vec3 p){
    vec3 a = floor(p);
    vec3 d = p - a;
    d = d * d * (3.0 - 2.0 * d);

    vec4 b = a.xxyy + vec4(0.0, 1.0, 0.0, 1.0);
    vec4 k1 = Perm(b.xyxy);
    vec4 k2 = Perm(k1.xyxy + b.zzww);

    vec4 c = k2 + a.zzzz;
    vec4 k3 = Perm(c);
    vec4 k4 = Perm(c + 1.0);

    vec4 o1 = fract(k3 * (1.0 / 41.0));
    vec4 o2 = fract(k4 * (1.0 / 41.0));

    vec4 o3 = o2 * d.z + o1 * (1.0 - d.z);
    vec2 o4 = o3.yw * d.x + o3.xz * (1.0 - d.x);

    return o4.y * d.y + o4.x * (1.0 - d.y);
}

#endif