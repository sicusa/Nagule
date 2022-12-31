#version 410 core

#include <nagule/common.glsl>
#include <nagule/transparency.glsl>

uniform sampler2D DiffuseTex;

in VertexOutput {
    vec2 TexCoord;
} i;

layout(location = 0) out vec4 Accum;
layout(location = 1) out float Reveal;

void main()
{
    vec2 tiledCoord = i.TexCoord * Tiling + Offset;
    vec4 color = Diffuse * texture(DiffuseTex, tiledCoord);

    Reveal = GetTransparencyWeight(color) * color.a;
    Accum = vec4(color.rgb * Reveal, color.a);
}