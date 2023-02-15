#version 410 core

#include <nagule/common.glsl>

properties {
    float Radius;
}

uniform sampler2D ColorTex;

in vec2 TexCoord;
out vec4 FragColor;

float SCurve (float x)
{
    x = x * 2.0 - 1.0;
    return -x * abs(x) * 0.5 + x + 0.5;
}

vec4 BlurV(sampler2D source, vec2 size, vec2 uv, float radius)
{
    if (radius < 1.0) {
        return texture(source, uv);
    }

    vec4 A = vec4(0.0); 
    vec4 C = vec4(0.0); 

    float height = 1.0 / size.y;
    float divisor = 0.0; 
    float weight = 0.0;
    float radiusMultiplier = 1.0 / radius;

    int sampleCount = int(radius);
    int halfSampleCount = sampleCount / 2;
    float delta = radius / halfSampleCount;

    for (float y = -halfSampleCount; y <= halfSampleCount; y++) {
        float s = y * delta;
        A = texture(source, uv + vec2(0.0, s * height));
        weight = SCurve(1.0 - (abs(s) * radiusMultiplier)); 
        C += A * weight; 
        divisor += weight; 
    }

    return vec4(C.r / divisor, C.g / divisor, C.b / divisor, 1.0);
}

void main()
{
    FragColor = BlurV(ColorTex, vec2(ViewportWidth, ViewportHeight), TexCoord, Radius * ViewportWidth);
}