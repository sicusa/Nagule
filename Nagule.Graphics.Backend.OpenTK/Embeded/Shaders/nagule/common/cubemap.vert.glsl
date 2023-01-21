#version 410 core

#include <nagule/common.glsl>

out vec3 TexCoord;

void main()
{
    gl_Position = vec4(QUAD_VERTEX, 1);
    TexCoord = mat3(Matrix_V) * (gl_Position * Matrix_P_Inv).xyz;
}