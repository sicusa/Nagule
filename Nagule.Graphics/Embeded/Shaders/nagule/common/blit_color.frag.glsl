#version 410 core

uniform sampler2D ColorBuffer;

in vec2 TexCoord;
out vec4 FragColor;

void main()
{
    FragColor = texture(ColorBuffer, TexCoord);
}