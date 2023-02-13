#ifndef BLIT_DEPTH
#define BLIT_DEPTH

vec3 BlitDepth(vec3 color)
{
    return texture(DepthTex, TexCoord).rgb;
}

#endif