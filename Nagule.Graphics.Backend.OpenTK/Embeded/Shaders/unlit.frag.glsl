#version 410 core

#include <nagule/common.glsl>

uniform sampler2D DiffuseTex;

in VertexOutput {
    vec2 TexCoord;
} i;

out vec4 FragColor;

void main()
{
    vec2 tiledCoord = i.TexCoord * Tiling + Offset;
    FragColor = Diffuse * texture(DiffuseTex, tiledCoord);
}