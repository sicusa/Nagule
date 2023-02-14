#version 410 core

properties {
    float Threshold = 0.5;
}

uniform sampler2D ColorTex;

in vec2 TexCoord;
out vec4 FragColor;

const vec3 LuminanceVector = vec3(0.2125, 0.7154, 0.0721);

void main()
{
    vec4 color = texture(ColorTex, TexCoord);

    float luminance = dot(LuminanceVector, color.rgb);
    luminance = max(0.0, luminance - Threshold);

    color.rgb *= sign(luminance);
    FragColor = color;
}