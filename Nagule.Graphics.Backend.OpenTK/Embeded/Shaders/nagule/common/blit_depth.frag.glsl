#version 410 core

uniform sampler2D DepthBuffer;

in vec2 TexCoord;

void main()
{
    gl_FragDepth = texture(DepthBuffer, TexCoord).r;
}