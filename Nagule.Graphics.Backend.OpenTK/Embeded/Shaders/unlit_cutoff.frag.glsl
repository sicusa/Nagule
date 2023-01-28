#version 410 core

#include <nagule/common.glsl>

properties
{
    vec4 Diffuse = vec4(1, 1, 1, 1);
    vec2 Tiling = vec2(1, 1);
    vec2 Offset = vec2(0, 0);
    float Threshold = 0.9;
}

uniform sampler2D DiffuseTex;

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    vec2 tiledCoord = TexCoord * Tiling + Offset;
    vec4 color = Diffuse * texture(DiffuseTex, tiledCoord);

    if (color.a < Threshold) {
        discard;
    }

    FragColor = color;
}