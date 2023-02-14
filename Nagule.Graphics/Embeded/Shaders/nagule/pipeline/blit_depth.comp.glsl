#ifndef BLIT_DEPTH
#define BLIT_DEPTH

vec3 BlitDepth(vec3 color)
{
    return vec3(texture(DepthTex, TexCoord).r) / 100;
}

#endif