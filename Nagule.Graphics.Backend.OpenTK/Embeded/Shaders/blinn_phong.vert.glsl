#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 vertex;
IN_TEXCOORD vec2 texCoord;
IN_NORMAL vec3 normal;

out VertexOutput {
    vec3 Position;
    vec2 TexCoord;
    vec3 Normal;
    float Depth;
} o;

void main()
{
    vec4 pos = vec4(vertex, 1) * ObjectToWorld;
    vec4 viewPos = pos * Matrix_V;

    gl_Position = viewPos * Matrix_P;
    o.Position = pos.xyz;
    o.TexCoord = texCoord;
    o.Normal = normalize(normal * mat3(ObjectToWorld));
    o.Depth = -viewPos.z;
}