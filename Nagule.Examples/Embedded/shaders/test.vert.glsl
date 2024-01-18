#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 Vertex;
IN_TEXCOORD vec2 TexCoord;

out VertexOutput {
    vec2 TexCoord;
    vec3 Position;
} o;

void main()
{
    vec4 pos = ObjectToWorld * vec4(Vertex, 1);
    o.Position = pos.xyz;
    o.TexCoord = TexCoord;
    gl_Position = Matrix_VP * pos;
}