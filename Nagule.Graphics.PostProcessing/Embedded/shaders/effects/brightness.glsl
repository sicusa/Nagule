#ifndef BRIGHTNESS
#define BRIGHTNESS

vec3 Brightness(vec3 color, float depth)
{
    return color * Brightness_Value;
}

#endif