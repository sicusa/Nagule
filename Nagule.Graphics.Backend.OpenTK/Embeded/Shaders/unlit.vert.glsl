#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 vertex;
IN_TEXCOORD vec2 texCoord;

out vec2 TexCoord;

void main()
{
    vec4 worldPos = vec4(vertex, 1) * ObjectToWorld;
    gl_Position = worldPos * Matrix_VP;

    TexCoord = texCoord;
}