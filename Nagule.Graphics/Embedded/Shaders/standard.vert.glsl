#version 410 core

#include <nagule/common.glsl>

IN_VERTEX vec3 Vertex;
IN_TEXCOORD vec2 TexCoord;

#ifndef LightingMode_Unlit
    IN_NORMAL vec3 Normal;
    #if defined(_NormalTex) || defined(_HeightTex)
        IN_TANGENT vec3 Tangent;
    #endif
#endif

out VertexOutput {
    vec2 TexCoord;

    #ifndef LightingMode_Unlit
        vec3 Position;
        #if defined(_NormalTex) || defined(_HeightTex)
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
    vec4 pos = ObjectToWorld * vec4(Vertex, 1);
    o.TexCoord = TexCoord;

    #ifndef LightingMode_Unlit
    {
        o.Position = pos.xyz;

        #if defined(_NormalTex) || defined(_HeightTex)
        {
            vec3 T = normalize(vec3(ObjectToWorld * vec4(Tangent, 0)));
            vec3 N = normalize(vec3(ObjectToWorld * vec4(Normal, 0)));
            T = normalize(T - dot(T, N) * N);
            vec3 B = cross(N, T);
            o.TBN = mat3(T, B, N);
        }
        #else
            o.Normal = normalize(vec3(ObjectToWorld * vec4(Normal, 0)));
        #endif
    }
    #endif

    #if defined(LightingMode_Full) || defined(LightingMode_Local)
    {
        vec4 viewPos = Matrix_V * pos;
        o.Depth = -viewPos.z;
        gl_Position = Matrix_P * viewPos;
    }
    #else
        gl_Position = Matrix_VP * pos;
    #endif
}