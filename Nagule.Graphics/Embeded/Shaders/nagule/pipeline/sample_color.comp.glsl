#ifndef SAMPLE_COLOR
#define SAMPLE_COLOR

vec3 SampleColor(vec3 color)
{
    return texture(ColorTex, TexCoord).rgb;
}

#endif