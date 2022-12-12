#version 410 core

uniform sampler2D AccumColorTex;
uniform sampler2D AccumAlphaTex;

out vec4 FragColor;

void main()
{
    ivec2 fragCoord = ivec2(gl_FragCoord.xy);
    vec4 accum = texelFetch(AccumColorTex, fragCoord, 0);
    float a = 1.0 - accum.a;
    accum.a = texelFetch(AccumAlphaTex, fragCoord, 0).r;
    FragColor = vec4(a * accum.rgb / clamp(accum.a, 0.001, 50000.0), a);
}