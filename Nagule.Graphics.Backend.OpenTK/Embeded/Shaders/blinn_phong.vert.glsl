#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 vertex;
IN_TEXCOORD vec2 texCoord;
#ifndef LightingMode_Unlit
IN_NORMAL vec3 normal;
#endif
#ifdef _NormalTex
IN_TANGENT vec3 tangent;
IN_BITANGENT vec3 bitangent;
#endif

out VertexOutput {
    vec2 TexCoord;

#ifndef LightingMode_Unlit
    vec3 Position;
#ifdef _NormalTex
    mat3 TBN;
#else
    vec3 Normal;
#endif
#endif

#if defined(LightingMode_Full) || defined(LightingMode_Local)
    float Depth;
#endif
} o;

void main()
{
    vec4 pos = ObjectToWorld * vec4(vertex, 1);
    o.TexCoord = texCoord;

#ifndef LightingMode_Unlit
    o.Position = pos.xyz;
    mat3 model = mat3(ObjectToWorld);
#ifdef _NormalTex
    vec3 T = normalize(model * tangent);
    vec3 N = normalize(model * normal);
    vec3 B = normalize(model * bitangent);
    o.TBN = mat3(T, B, N);
#else
    o.Normal = normalize(model * normal);
#endif
#endif

#if defined(LightingMode_Full) || defined(LightingMode_Local)
    vec4 viewPos = Matrix_V * pos;
    o.Depth = -viewPos.z;
    gl_Position = Matrix_P * viewPos;
#else
    gl_Position = Matrix_VP * pos;
#endif
}