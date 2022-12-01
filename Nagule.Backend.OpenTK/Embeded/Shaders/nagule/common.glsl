#ifndef AECO_COMMON
#define AECO_COMMON

layout(std140) uniform Framebuffer {
    int ViewportWidth;
    int ViewportHeight;
};

layout(std140) uniform Camera {
    mat4 Matrix_V;
    mat4 Matrix_P;
    mat4 Matrix_VP;
    vec3 CameraPosition;
    float CameraNearPlaneDistance;
    float CameraFarPlaneDistance;
};

layout(std140) uniform Material {
    vec4 Diffuse;
    vec4 Specular;
    vec4 Ambient;
    vec4 Emission;
    float Shininess;
    float Reflectivity;
    vec2 Tiling;
    vec2 Offset;
};

layout(std140) uniform Mesh {
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

vec3 GetClipSpacePositionFromDepth(float depth, vec2 uv)
{
    float z = depth * 2.0 - 1.0;
    return vec3(uv * 2.0 - 1.0, z);
}

vec4 GetViewSpacePositionFromDepth(float depth, vec2 uv)
{
    float z = depth * 2.0 - 1.0;
    vec4 clipPos = vec4(uv * 2.0 - 1.0, z, 1.0);
    vec4 viewPos = clipPos * inverse(Matrix_P);
    viewPos.xyz /= viewPos.w;
    return viewPos;
}

vec4 GetWorldSpacePositionFromDepth(float depth, vec2 uv)
{
    float z = depth * 2.0 - 1.0;
    vec4 clipPos = vec4(uv * 2.0 - 1.0, z, 1.0);
    vec4 viewPos = clipPos * inverse(Matrix_P);
    viewPos.xyz /= viewPos.w;
    return viewPos * inverse(Matrix_V);
}

#endif