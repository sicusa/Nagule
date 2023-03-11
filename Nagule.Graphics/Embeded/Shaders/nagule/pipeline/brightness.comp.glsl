#ifndef BRIGHTNESS
#define BRIGHTNESS

vec3 Brightness(vec3 color)
{
    return color * Brightness_Value;
}

#endif