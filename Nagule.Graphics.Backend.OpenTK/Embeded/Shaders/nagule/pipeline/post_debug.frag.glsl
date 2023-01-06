#version 410 core

#include <nagule/lighting.glsl>

uniform sampler2D ColorBuffer;
uniform sampler2D TransparencyAccumBuffer;
uniform sampler2D TransparencyRevealBuffer;
uniform sampler2D DepthBuffer;

in vec2 TexCoord;
out vec4 FragColor;

subroutine vec3 PostFunc();
subroutine uniform PostFunc PostFuncUniform;

subroutine(PostFunc) vec3 ShowColor()
{
    return texture(ColorBuffer, TexCoord).rgb;
}

subroutine(PostFunc) vec3 ShowDepth()
{
    float depth = LinearizeDepth(texture(DepthBuffer, TexCoord, 0).r) / 30;
    return vec3(depth, depth, depth);
}

subroutine(PostFunc) vec3 ShowTransparencyAccum() {
    return texture(TransparencyAccumBuffer, TexCoord).rgb;
}

subroutine(PostFunc) vec3 ShowTransparencyReveal() {
    return vec3(texture(TransparencyRevealBuffer, TexCoord).r);
}

subroutine(PostFunc) vec3 ShowClusters() {
    ivec2 texCoord = ivec2(gl_FragCoord.xy);
    float depth = LinearizeDepth(texelFetch(DepthBuffer, texCoord, 0).r);
    int index = GetClusterIndex(texCoord, depth);
    float c = float(FetchLightCount(index)) / MAXIMUM_CLUSTER_LIGHT_COUNT;
    return vec3(c);
}

vec3 ACESToneMapping(vec3 color)
{
    const float A = 2.51f;
    const float B = 0.03f;
    const float C = 2.43f;
    const float D = 0.59f;
    const float E = 0.14f;
    return (color * (A * color + B)) / (color * (C * color + D) + E);
}

void main()
{
    const float gamma = 2.2;
    vec3 hdrColor = PostFuncUniform();
  
    // tone mapping
    vec3 mapped = ACESToneMapping(hdrColor);

    // gamma correction 
    mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}