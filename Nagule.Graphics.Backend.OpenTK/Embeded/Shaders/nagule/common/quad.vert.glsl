#version 410 core

#include <nagule/common.glsl>

out vec2 TexCoord;

void main()
{
    TexCoord = QUAD_TEXCOORD;
    gl_Position = vec4(QUAD_VERTEX, 1);
}