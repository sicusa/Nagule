#version 410 core
#include <nagule/common.glsl>

uniform sampler2D DepthBuffer;

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    float depth = texture(DepthBuffer, TexCoord).r;
    FragColor = vec4(depth);
}