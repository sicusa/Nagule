#version 410 core

#include <nagule/common.glsl>

in mat4 mvp;
in vec4 point;
out vec4 FragColor;

void main()
{
    vec4 boundingBox[8];

    /* create the bounding box of the object in world space */
    boundingBox[0] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMax.y, BoundingBoxMax.z, 1.0);
    boundingBox[1] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMax.y, BoundingBoxMax.z, 1.0);
    boundingBox[2] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMin.y, BoundingBoxMax.z, 1.0);
    boundingBox[3] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMin.y, BoundingBoxMax.z, 1.0);
    boundingBox[4] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMax.y, BoundingBoxMin.z, 1.0);
    boundingBox[5] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMax.y, BoundingBoxMin.z, 1.0);
    boundingBox[6] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMin.y, BoundingBoxMin.z, 1.0);
    boundingBox[7] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMin.y, BoundingBoxMin.z, 1.0);
	
	/* perform perspe;ctive division for the bounding box */
    float minW = POSITIVE_INFINITY;
	for (int i = 0; i < 8; i++) {
        float w = boundingBox[i].w;
		boundingBox[i].xyz /= w;
        minW = min(minW, w);
    }
    if (minW <= 0) {
        return 0;
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

	depth = LinearizeDepth(depth) / 10;
    FragColor = vec4(color / 10);
}