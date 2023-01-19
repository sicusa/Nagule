#version 410 core

#include <nagule/common.glsl>
#include <nagule/instancing.glsl>

layout(location = 0) in vec3 vertex;

void main()
{
    ENABLE_INSTANCING;
    gl_Position = vec4(vertex, 1) * ObjectToWorld * Matrix_VP;
}