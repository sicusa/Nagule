#version 410 core

uniform samplerCube SkyboxTex;

in vec3 TexCoord;
out vec4 FragColor;

void main()
{
    FragColor = texture(SkyboxTex, TexCoord);
}