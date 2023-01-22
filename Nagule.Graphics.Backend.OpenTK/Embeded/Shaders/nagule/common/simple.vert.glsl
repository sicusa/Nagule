#version 410 core

#include <nagule/common.glsl>

LOC_VERTEX in vec3 vertex;

void main()
{
    gl_Position = vec4(vertex, 1) * ObjectToWorld * Matrix_VP;
}