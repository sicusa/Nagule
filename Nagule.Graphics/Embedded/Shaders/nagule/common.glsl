#ifndef NAGULE_COMMON
#define NAGULE_COMMON

const float POSITIVE_INFINITY = 1. / 0.;
const float NEGATIVE_INFINITY = -1. / 0.;
const float PI = 3.14159265358979323846;
const float TAU = PI * 2;

const vec3 QUAD_VERTICES[4] = vec3[4](
    vec3(-1.0, -1.0, 1.0),
    vec3( 1.0, -1.0, 1.0),
    vec3(-1.0,  1.0, 1.0),
    vec3( 1.0,  1.0, 1.0));

const vec2 QUAD_TEXCOORDS[4] = vec2[4](
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(0.0, 1.0),
    vec2(1.0, 1.0));

#define QUAD_VERTEX QUAD_VERTICES[gl_VertexID]
#define QUAD_TEXCOORD QUAD_TEXCOORDS[gl_VertexID]

#define IN_VERTEX layout(location = 0) in
#define IN_TEXCOORD layout(location = 1) in
#define IN_NORMAL layout(location = 2) in
#define IN_TANGENT layout(location = 3) in

layout(location = 4) in mat4 ObjectToWorld;

layout(std140) uniform Pipeline
{
    int ViewportWidth;
    int ViewportHeight;
    float Time;
};

layout(std140) uniform Camera
{
    mat4 Matrix_V;
    mat4 Matrix_P;
    mat4 Matrix_P_Inv;
    mat4 Matrix_VP;

    vec3 CameraPosition;
    float CameraNearPlaneDistance;
    float CameraFarPlaneDistance;
};

layout(std140) uniform Mesh
{
    vec3 BoundingBoxMin;
    vec3 BoundingBoxMax;
};

float LinearizeDepth(float depth)
{
    float n = CameraNearPlaneDistance;
    float f = CameraFarPlaneDistance;
    float depthRange = 2.0 * depth - 1.0;
    return 2.0 * n * f / (f + n - depthRange * (f - n));
}

vec3 GetClipPositionFromDepth(float depth, vec2 uv)
{
    float z = depth * 2.0 - 1.0;
    return vec3(uv * 2.0 - 1.0, z);
}

vec4 GetViewPositionFromDepth(float depth, vec2 uv)
{
    float z = depth * 2.0 - 1.0;
    vec4 clipPos = vec4(uv * 2.0 - 1.0, z, 1.0);
    vec4 viewPos = Matrix_P_Inv * clipPos;
    viewPos.xyz /= viewPos.w;
    return viewPos;
}

vec4 GetWorldPositionFromDepth(float depth, vec2 uv) {
    return GetViewPositionFromDepth(depth, uv) * Matrix_V;
}

#endif