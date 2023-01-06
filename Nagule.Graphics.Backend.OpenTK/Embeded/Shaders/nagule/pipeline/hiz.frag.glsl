#version 410 core

uniform sampler2D LastMip;

in vec2 TexCoord;

void main()
{
    vec4 texels = textureGather(LastMip, TexCoord, 0);
    float maxZ = max(max(texels.x, texels.y), max(texels.z, texels.w));
    gl_FragDepth = maxZ;
}
