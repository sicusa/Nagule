#version 410 core

#include <nagule/common.glsl>

uniform sampler2D DiffuseTex;

uniform float Threshold;

in VertexOutput {
    vec2 TexCoord;
} i;

out vec4 FragColor;

void main()
{
    vec2 tiledCoord = i.TexCoord * Tiling + Offset;
    vec4 color = Diffuse * texture(DiffuseTex, tiledCoord);

    if (color.a < Threshold) {
        discard;
    }

    FragColor = color;
}