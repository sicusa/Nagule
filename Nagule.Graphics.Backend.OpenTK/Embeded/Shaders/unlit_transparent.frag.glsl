#version 410 core

#include <nagule/common.glsl>
#include <nagule/transparency.glsl>

properties
{
    vec4 Diffuse = vec4(1, 1, 1, 1);
    vec2 Tiling = vec2(1, 1);
    vec2 Offset = vec2(0, 0);
}

uniform sampler2D DiffuseTex;

in vec2 TexCoord;

OUT_ACCUM vec4 Accum;
OUT_REVEAL float Reveal;

void main()
{
    vec2 tiledCoord = TexCoord * Tiling + Offset;
    vec4 color = Diffuse * texture(DiffuseTex, tiledCoord);

    Reveal = GetTransparencyWeight(color) * color.a;
    Accum = vec4(color.rgb * Reveal, color.a);
}