#version 410 core

#include <nagule/common.glsl>

layout(location = 0) in mat4 ObjectToWorld;

out mat4 OriginalObjectToWorld;
flat out int ObjectVisible;

uniform sampler2D DepthBuffer;

vec4 boundingBox[8];

bool InstanceCloudReduction()
{
    /* calculate MVP matrix */
    mat4 mvp = ObjectToWorld * Matrix_VP;

    /* create the bounding box of the object in world space */
    boundingBox[0] = vec4(BoundingBoxMax.x, BoundingBoxMax.y, BoundingBoxMax.z, 1.0) * mvp;
    boundingBox[1] = vec4(BoundingBoxMin.x, BoundingBoxMax.y, BoundingBoxMax.z, 1.0) * mvp;
    boundingBox[2] = vec4(BoundingBoxMax.x, BoundingBoxMin.y, BoundingBoxMax.z, 1.0) * mvp;
    boundingBox[3] = vec4(BoundingBoxMin.x, BoundingBoxMin.y, BoundingBoxMax.z, 1.0) * mvp;
    boundingBox[4] = vec4(BoundingBoxMax.x, BoundingBoxMax.y, BoundingBoxMin.z, 1.0) * mvp;
    boundingBox[5] = vec4(BoundingBoxMin.x, BoundingBoxMax.y, BoundingBoxMin.z, 1.0) * mvp;
    boundingBox[6] = vec4(BoundingBoxMax.x, BoundingBoxMin.y, BoundingBoxMin.z, 1.0) * mvp;
    boundingBox[7] = vec4(BoundingBoxMin.x, BoundingBoxMin.y, BoundingBoxMin.z, 1.0) * mvp;

    /* check how the bounding box resides regarding to the view frustum */   
    int outOfBound[6] = int[6](0, 0, 0, 0, 0, 0);

    for (int i = 0; i < 8; i++) {
        if (boundingBox[i].x >  boundingBox[i].w) outOfBound[0]++;
        if (boundingBox[i].x < -boundingBox[i].w) outOfBound[1]++;
        if (boundingBox[i].y >  boundingBox[i].w) outOfBound[2]++;
        if (boundingBox[i].y < -boundingBox[i].w) outOfBound[3]++;
        if (boundingBox[i].z >  boundingBox[i].w) outOfBound[4]++;
        if (boundingBox[i].z < -boundingBox[i].w) outOfBound[5]++;
    }

    bool inFrustum = true;

    for (int i = 0; i < 6; ++i) {
        if (outOfBound[i] == 8) {
            inFrustum = false;
        }
    }

    return inFrustum;
}

bool HiZOcclusionCull()
{
	/* perform perspective division for the bounding box */
	for (int i = 0; i < 8; i++) {
		boundingBox[i].xyz /= boundingBox[i].w;
    }
	
	/* calculate screen space bounding rectangle */
	vec2 boundingRect[2];
	boundingRect[0].x = min(min(min(boundingBox[0].x, boundingBox[1].x),
                                min(boundingBox[2].x, boundingBox[3].x)),
                            min(min(boundingBox[4].x, boundingBox[5].x),
                                min(boundingBox[6].x, boundingBox[7].x))) / 2.0 + 0.5;
	boundingRect[0].y = min(min(min(boundingBox[0].y, boundingBox[1].y),
                                min(boundingBox[2].y, boundingBox[3].y)),
                            min(min(boundingBox[4].y, boundingBox[5].y),
                                min(boundingBox[6].y, boundingBox[7].y))) / 2.0 + 0.5;
	boundingRect[1].x = max(max(max(boundingBox[0].x, boundingBox[1].x),
                                max(boundingBox[2].x, boundingBox[3].x)),
                            max(max(boundingBox[4].x, boundingBox[5].x),
                                max(boundingBox[6].x, boundingBox[7].x))) / 2.0 + 0.5;
	boundingRect[1].y = max(max(max(boundingBox[0].y, boundingBox[1].y),
                                max(boundingBox[2].y, boundingBox[3].y)),
                            max(max(boundingBox[4].y, boundingBox[5].y),
                                max(boundingBox[6].y, boundingBox[7].y))) / 2.0 + 0.5;

	/* then the linear depth value of the front-most point */
	float depth = min(min(min(boundingBox[0].z, boundingBox[1].z),
                          min(boundingBox[2].z, boundingBox[3].z)),
                      min(min(boundingBox[4].z, boundingBox[5].z),
                          min(boundingBox[6].z, boundingBox[7].z)));

	/* now we calculate the bounding rectangle size in viewport coordinates */
	float viewSizeX = (boundingRect[1].x - boundingRect[0].x) * ViewportWidth;
	float viewSizeY = (boundingRect[1].y - boundingRect[0].y) * ViewportHeight;
	
	/* now we calculate the texture LOD used for lookup in the depth buffer texture */
	float LOD = ceil(log2(max(viewSizeX, viewSizeY) / 2.0));
	
	/* finally fetch the depth texture using explicit LOD lookups */
	vec4 samples;
	samples.x = textureLod(DepthBuffer, vec2(boundingRect[0].x, boundingRect[0].y), LOD).x;
	samples.y = textureLod(DepthBuffer, vec2(boundingRect[0].x, boundingRect[1].y), LOD).x;
	samples.z = textureLod(DepthBuffer, vec2(boundingRect[1].x, boundingRect[1].y), LOD).x;
	samples.w = textureLod(DepthBuffer, vec2(boundingRect[1].x, boundingRect[0].y), LOD).x;
	float maxDepth = max(max(samples.x, samples.y), max(samples.z, samples.w));
	
	/* if the instance depth is bigger than the depth in the texture discard the instance */
	return depth <= maxDepth;
}

void main()
{
    OriginalObjectToWorld = ObjectToWorld;
    ObjectVisible = ObjectToWorld[0][0] != POSITIVE_INFINITY
        && InstanceCloudReduction()
        && HiZOcclusionCull() ? 1 : 0;
}
