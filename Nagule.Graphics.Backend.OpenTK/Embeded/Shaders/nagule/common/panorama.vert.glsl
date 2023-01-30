#version 410 core

#include <nagule/common.glsl>

out vec3 TexCoord;

void main()
{
    gl_Position = vec4(QUAD_VERTEX, 1);
    TexCoord = (Matrix_P_Inv * gl_Position).xyz * mat3(Matrix_V);
}