#version 410 core

uniform sampler2D AccumTex;
uniform sampler2D RevealTex;

out vec4 FragColor;

void main()
{
    ivec2 fragCoord = ivec2(gl_FragCoord.xy);
    vec4 accum = texelFetch(AccumTex, fragCoord, 0);
    float a = 1.0 - accum.a;
    accum.a = texelFetch(RevealTex, fragCoord, 0).r;
    FragColor = vec4(a * accum.rgb / clamp(accum.a, 0.001, 50000.0), a);
}