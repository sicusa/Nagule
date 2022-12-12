#version 410 core

#include <nagule/common.glsl>
#include <nagule/instancing.glsl>

layout(location = 0) in vec3 vertex;
layout(location = 1) in vec2 texCoord;
layout(location = 2) in vec3 normal;
layout(location = 3) in vec3 tangent;

out VertexOutput {
    vec3 Position;
    vec2 TexCoord;
    vec3 Normal;
    float Depth;
} o;

void main()
{
    ENABLE_INSTANCING;

    vec4 pos = vec4(vertex, 1) * ObjectToWorld;
    vec4 viewPos = pos * Matrix_V;

    gl_Position = viewPos * Matrix_P;
    o.Position = pos.xyz;
    o.TexCoord = texCoord;
    o.Normal = normalize(normal * mat3(ObjectToWorld));
    o.Depth = -viewPos.z;
}