#ifndef BLOOM
#define BLOOM

#include <nagule/common.glsl>
#include <nagule/noise.glsl>

float SCurve(float x)
{
    x = x * 2.0 - 1.0;
    return -x * abs(x) * 0.5 + x + 0.5;
}

vec4 BlurH(sampler2D source, vec2 size, vec2 uv, float radius)
{
    if (radius < 1.0) {
        return texture(source, uv);
    }

    vec4 A = vec4(0.0); 
    vec4 C = vec4(0.0); 

    float width = 1.0 / size.x;
    float divisor = 0.0; 
    float weight = 0.0;
    float radiusMultiplier = 1.0 / radius;

    int sampleCount = int(radius);
    int halfSampleCount = sampleCount / 2;
    float delta = radius / halfSampleCount;

    for (float x = -halfSampleCount; x <= halfSampleCount; x++) {
        float s = x * delta;
        A = texture(source, uv + vec2(s * width, 0));
        weight = SCurve(1.0 - (abs(s) * radiusMultiplier)); 
        C += A * weight; 
        divisor += weight; 
    }

    return vec4(C.r / divisor, C.g / divisor, C.b / divisor, 1.0);
}

vec3 Bloom(vec3 color)
{
    vec4 bloom = BlurH(Bloom_BrightnessTex, vec2(ViewportWidth, ViewportHeight), TexCoord, Bloom_Radius * ViewportWidth)
        * Bloom_Intensity;

#ifdef _Bloom_DirtTex
    bloom = bloom + Bloom_DirtIntensity * bloom * texture(Bloom_DirtTex);
#endif

    return mix(color, bloom.rgb, vec3(0.1 / Bloom_Threshold));
}

#endif