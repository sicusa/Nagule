#ifndef NAGULE_LIGHTING
#define NAGULE_LIGHTING

#include <nagule/common.glsl>

#define MAXIMUM_GLOBAL_LIGHT_COUNT 8
#define CLUSTER_COUNT_X 16
#define CLUSTER_COUNT_Y 9
#define CLUSTER_COUNT_Z 24
#define CLUSTER_COUNT (CLUSTER_COUNT_X * CLUSTER_COUNT_Y * CLUSTER_COUNT_Z)
#define MAXIMUM_CLUSTER_LIGHT_COUNT 1024

#define LIGHT_NONE          0
#define LIGHT_AMBIENT       1
#define LIGHT_DIRECTIONAL   2
#define LIGHT_POINT         3
#define LIGHT_SPOT          4
#define LIGHT_AREA          5

#define LIGHT_COMPONENT_COUNT 14

layout(std140) uniform LightClusters
{
    float ClusterDepthSliceMultiplier;
    float ClusterDepthSliceSubstractor;

    int GlobalLightCount;
    int GlobalLightIndices[MAXIMUM_GLOBAL_LIGHT_COUNT];
};

struct Light
{
    int Category;
    vec4 Color;
    vec3 Position;
    float Range;
    vec3 Direction;
    vec2 ConeCutoffsOrAreaSize;
};

uniform samplerBuffer LightsBuffer;
uniform isamplerBuffer ClustersBuffer;
uniform isamplerBuffer ClusterLightCountsBuffer;

int CalculateClusterDepthSlice(float z) {
    return int(max(log2(z) * ClusterDepthSliceMultiplier - ClusterDepthSliceSubstractor, 0.0));
}

int GetClusterIndex(vec2 fragCoord, float depth)
{
    int depthSlice = CalculateClusterDepthSlice(depth);
    float tileSizeX = ViewportWidth / CLUSTER_COUNT_X;
    float tileSizeY = ViewportHeight / CLUSTER_COUNT_Y;

    return int(fragCoord.x / tileSizeX)
        + CLUSTER_COUNT_X * int(fragCoord.y / tileSizeY)
        + (CLUSTER_COUNT_X * CLUSTER_COUNT_Y) * depthSlice;
}

int FetchLightIndex(int cluster, int offset) {
    return texelFetch(ClustersBuffer, cluster * MAXIMUM_CLUSTER_LIGHT_COUNT + offset).r;
}

int FetchLightCount(int cluster) {
    return texelFetch(ClusterLightCountsBuffer, cluster).r;
}

Light FetchGlobalLight(int index)
{
    int offset = index * LIGHT_COMPONENT_COUNT;
    Light light;

    int category = int(texelFetch(LightsBuffer, offset).r);
    light.Category = category;

    light.Color = vec4(
        texelFetch(LightsBuffer, offset + 1).r,
        texelFetch(LightsBuffer, offset + 2).r,
        texelFetch(LightsBuffer, offset + 3).r,
        texelFetch(LightsBuffer, offset + 4).r);
    
    if (category == LIGHT_DIRECTIONAL) {
        light.Direction = vec3(
            texelFetch(LightsBuffer, offset + 9).r,
            texelFetch(LightsBuffer, offset + 10).r,
            texelFetch(LightsBuffer, offset + 11).r);
    }

    return light;
}

Light FetchLight(int index)
{
    int offset = index * LIGHT_COMPONENT_COUNT;
    Light light;

    int category = int(texelFetch(LightsBuffer, offset).r);
    light.Category = category;

    light.Color = vec4(
        texelFetch(LightsBuffer, offset + 1).r,
        texelFetch(LightsBuffer, offset + 2).r,
        texelFetch(LightsBuffer, offset + 3).r,
        texelFetch(LightsBuffer, offset + 4).r);

    light.Position = vec3(
        texelFetch(LightsBuffer, offset + 5).r,
        texelFetch(LightsBuffer, offset + 6).r,
        texelFetch(LightsBuffer, offset + 7).r);

    light.Range = texelFetch(LightsBuffer, offset + 8).r;
    
    if (category != LIGHT_POINT) {
        light.Direction = vec3(
            texelFetch(LightsBuffer, offset + 9).r,
            texelFetch(LightsBuffer, offset + 10).r,
            texelFetch(LightsBuffer, offset + 11).r);

        light.ConeCutoffsOrAreaSize = vec2(
            texelFetch(LightsBuffer, offset + 12).r,
            texelFetch(LightsBuffer, offset + 13).r);
    }

    return light;
}

Light FetchLightFromCluster(int cluster, int offset) {
    return FetchLight(FetchLightIndex(cluster, offset));
}

float CalculateLightAttenuation(float range, float distance) {
    return 1 / (1 + distance * distance) * smoothstep(range, 0, distance);
}

#endif