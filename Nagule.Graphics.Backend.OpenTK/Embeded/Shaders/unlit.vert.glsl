#version 410 core

#include <nagule/common.glsl>
#include <nagule/instancing.glsl>

layout(location = 0) in vec3 vertex;
layout(location = 1) in vec2 texCoord;

out VertexOutput {
    vec2 TexCoord;
} o;

void main()
{
    ENABLE_INSTANCING;

    vec4 worldPos = vec4(vertex, 1) * ObjectToWorld;
    gl_Position = worldPos * Matrix_VP;

    o.TexCoord = texCoord;
}