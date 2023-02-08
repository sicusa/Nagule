#version 410 core

#include <nagule/common.glsl>

out mat4 OriginalObjectToWorld;
flat out int ObjectVisible;

vec4 boundingBox[8];

bool InstanceCloudReduction()
{
    /* calculate MVP matrix */
    mat4 mvp = Matrix_VP * ObjectToWorld;

    /* create the bounding box of the object in world space */
    boundingBox[0] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMax.y, BoundingBoxMax.z, 1.0);
    boundingBox[1] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMax.y, BoundingBoxMax.z, 1.0);
    boundingBox[2] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMin.y, BoundingBoxMax.z, 1.0);
    boundingBox[3] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMin.y, BoundingBoxMax.z, 1.0);
    boundingBox[4] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMax.y, BoundingBoxMin.z, 1.0);
    boundingBox[5] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMax.y, BoundingBoxMin.z, 1.0);
    boundingBox[6] = mvp * vec4(BoundingBoxMax.x, BoundingBoxMin.y, BoundingBoxMin.z, 1.0);
    boundingBox[7] = mvp * vec4(BoundingBoxMin.x, BoundingBoxMin.y, BoundingBoxMin.z, 1.0);

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

void main()
{
    OriginalObjectToWorld = ObjectToWorld;
    ObjectVisible = InstanceCloudReduction() ? 1 : 0;
}
