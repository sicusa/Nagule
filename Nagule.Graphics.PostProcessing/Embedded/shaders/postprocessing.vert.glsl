#version 410 core

#include <nagule/common.glsl>

out vec2 TexCoord;
out vec4 Vertex;
out vec3 EyeDirection;

void main()
{
    TexCoord = QUAD_TEXCOORD;
    Vertex = vec4(QUAD_VERTEX, 1);
    EyeDirection = (Matrix_P_Inv * Vertex).xyz * mat3(Matrix_V);
    gl_Position = Vertex;
}