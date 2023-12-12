#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 vertex;

void main()
{
    gl_Position = Matrix_VP * ObjectToWorld * vec4(vertex, 1);
}