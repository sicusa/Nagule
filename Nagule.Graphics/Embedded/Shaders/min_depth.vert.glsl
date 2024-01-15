#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 Vertex;
out vec4 Point;
out mat4 MVP;

void main()
{
    MVP = Matrix_VP * ObjectToWorld;
    Point = MVP * vec4(Vertex, 1);
    gl_Position = Point;
}