#version 410 core

uniform sampler2D AccumTex;
uniform sampler2D RevealTex;

out vec4 FragColor;

const float EPSILON = 0.00001;

bool IsApproximatelyEqual(float a, float b) {
    return abs(a - b) <= (abs(a) < abs(b) ? abs(b) : abs(a)) * EPSILON;
}

void main()
{
    ivec2 fragCoord = ivec2(gl_FragCoord.xy);
    float reveal = texelFetch(RevealTex, fragCoord, 0).r;

    if (IsApproximatelyEqual(reveal, 1)) {
        discard;
    }
    vec4 accum = texelFetch(AccumTex, fragCoord, 0);
    FragColor = vec4(accum.rgb / clamp(accum.a, 0.00001, 50000.0), 1 - reveal);
}