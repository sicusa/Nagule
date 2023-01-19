#version 410 core

#include <nagule/common.glsl>

out vec3 TexCoord;

void main()
{
    o.TexCoord = mat3(Matrix_V) * QUAD_VERTEX;
    gl_Position = vec4(QUAD_VERTEX, 1);
}