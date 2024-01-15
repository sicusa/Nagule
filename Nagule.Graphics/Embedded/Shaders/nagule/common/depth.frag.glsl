#version 410 core

layout(location = 0) out float FragDepth;

void main()
{
    dragDepth = gl_FragCoord.z;
}