#version 410 core

uniform sampler2D ColorBuffer;

in vec2 TexCoord;
out vec4 FragColor;

vec3 ACESToneMapping(vec3 color)
{
    const float A = 2.51f;
    const float B = 0.03f;
    const float C = 2.43f;
    const float D = 0.59f;
    const float E = 0.14f;
    return (color * (A * color + B)) / (color * (C * color + D) + E);
}

void main()
{
    const float gamma = 2.2;
    vec3 hdrColor = texture(ColorBuffer, TexCoord).rgb;
  
    // tone mapping
    vec3 mapped = ACESToneMapping(hdrColor);

    // gamma correction 
    mapped = pow(mapped, vec3(1.0 / gamma));

    FragColor = vec4(mapped, 1.0);
}