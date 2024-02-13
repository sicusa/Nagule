#version 410 core

layout(points) in;
layout(points, max_vertices = 1) out;

flat in mat4 OriginalObjectToWorld[1];
flat in int OriginalLayerMask[1];
flat in int ObjectVisible[1];

out mat4 CulledObjectToWorld;
out int CulledLayerMask;

void main()
{
    if (ObjectVisible[0] == 1) {
        CulledObjectToWorld = OriginalObjectToWorld[0];
        CulledLayerMask = OriginalLayerMask[0];
        EmitVertex();
        EndPrimitive();
    }
}