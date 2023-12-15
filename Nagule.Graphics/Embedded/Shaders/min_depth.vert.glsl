#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 vertex;
out vec4 point;
out mat4 mvp;

void main()
{
    mvp = Matrix_VP * ObjectToWorld;
    point = mvp * vec4(vertex, 1);
    gl_Position = point;
}