#ifndef BLIT_COLOR
#define BLIT_COLOR

vec3 BlitColor(vec3 color)
{
    return texture(ColorTex, TexCoord).rgb;
}

#endif