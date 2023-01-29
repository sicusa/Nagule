#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 vertex;
IN_TEXCOORD vec2 texCoord;

#ifndef LightingMode_Unlit
IN_NORMAL vec3 normal;
#endif

out VertexOutput {
    vec2 TexCoord;
#ifndef LightingMode_Unlit
    vec3 Position;
    vec3 Normal;
#endif
#if defined(LightingMode_Full) || defined(LightingMode_Local)
    float Depth;
#endif
} o;

void main()
{
    vec4 pos = vec4(vertex, 1) * ObjectToWorld;
    o.TexCoord = texCoord;

#ifndef LightingMode_Unlit
    o.Position = pos.xyz;
    o.Normal = normalize(normal * mat3(ObjectToWorld));
#endif

#if defined(LightingMode_Full) || defined(LightingMode_Local)
    vec4 viewPos = pos * Matrix_V;
    o.Depth = -viewPos.z;
    gl_Position = viewPos * Matrix_P;
#else
    gl_Position = pos * Matrix_VP;
#endif
}