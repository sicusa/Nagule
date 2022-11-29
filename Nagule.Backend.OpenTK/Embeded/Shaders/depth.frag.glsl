#version 410 core

layout(location = 0) out float fragDepth;

void main()
{
    dragDepth = gl_FragCoord.z;
}