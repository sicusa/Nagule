#version 410 core
#include <nagule/common.glsl>

uniform sampler2D DepthBuffer;

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    float depth = LinearizeDepth(texture(DepthBuffer, TexCoord, 0).r) / 50;
    FragColor = vec4(depth);
}