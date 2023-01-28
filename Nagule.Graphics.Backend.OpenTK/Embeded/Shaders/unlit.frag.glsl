#version 410 core

#include <nagule/common.glsl>

properties
{
    vec4 Diffuse = vec4(1, 1, 1, 1);
    vec2 Tiling = vec2(1, 1);
    vec2 Offset = vec2(0, 0);
}

uniform sampler2D DiffuseTex;

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    vec2 tiledCoord = TexCoord * Tiling + Offset;
    FragColor = Diffuse * texture(DiffuseTex, tiledCoord);
}