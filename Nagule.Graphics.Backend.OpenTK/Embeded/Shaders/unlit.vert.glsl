#version 410 core

#include <nagule/common.glsl>

LOC_VERTEX   in vec3 vertex;
LOC_TEXCOORD in vec2 texCoord;

out VertexOutput {
    vec2 TexCoord;
} o;

void main()
{
    vec4 worldPos = vec4(vertex, 1) * ObjectToWorld;
    gl_Position = worldPos * Matrix_VP;

    o.TexCoord = texCoord;
}