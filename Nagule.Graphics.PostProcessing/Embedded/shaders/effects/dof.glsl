#ifndef NAGULE_DOF
#define NAGULE_DOF

#include <nagule/common.glsl>

#include <lygia/sample/clamp2edge.glsl>

#define SAMPLEDOF_BLUR_SIZE DepthOfField_BlurSize
#define SAMPLEDOF_RAD_SCALE DepthOfField_RadiusScale
#define SAMPLEDOF_COLOR_SAMPLE_FNC(TEX, UV) sampleClamp2edge(TEX, UV).rgb
#define SAMPLEDOF_DEPTH_SAMPLE_FNC(TEX, UV) LinearizeDepth(sampleClamp2edge(TEX, UV).r)
#define RESOLUTION vec2(ViewportWidth, ViewportHeight)

#include <lygia/sample/dof.glsl>

vec3 DepthOfField(vec3 color, float depth)
{
    return sampleDoF(ColorTex, DepthTex, TexCoord,
        DepthOfField_Focus, DepthOfField_FocusScale);
}

#endif