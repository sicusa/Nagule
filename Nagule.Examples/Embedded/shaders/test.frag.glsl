#version 410 core

#include <nagule/common.glsl>

uniform sampler2D DiffuseTex;
uniform sampler2DArray TestArrayTex;
uniform sampler2DArray TestTilesetTex;

in VertexOutput {
    vec2 TexCoord;
    vec3 Position;
} i;

out vec4 FragColor;

void main()
{
    //FragColor = texture(DiffuseTex, i.TexCoord);
    FragColor = texture(TestTilesetTex, vec3(i.TexCoord, Time));
}