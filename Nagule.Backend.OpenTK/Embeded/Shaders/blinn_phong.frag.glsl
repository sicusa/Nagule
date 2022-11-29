#version 410 core

#include <nagule/blinn_phong.glsl>

in VertexOutput {
    vec3 Position;
    vec2 TexCoord;
    vec3 Normal;
    float Depth;
} i;

out vec4 FragColor;

void main()
{
    FragColor = CalculateBlinnPhongLighting(i.Position, i.TexCoord, i.Normal, i.Depth);
}