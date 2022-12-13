#version 410 core

#include <nagule/blinn_phong.glsl>

uniform sampler2D DiffuseTex;
uniform sampler2D SpecularTex;
uniform sampler2D EmissionTex;

uniform float Threshold;

in VertexOutput {
    vec3 Position;
    vec2 TexCoord;
    vec3 Normal;
    float Depth;
} i;

out vec4 FragColor;

void main()
{
    vec2 tiledCoord = i.TexCoord * Tiling;

    vec4 diffuseColor = Diffuse * texture(DiffuseTex, tiledCoord);
    if (diffuseColor.a < Threshold) {
        discard;
    }

    vec4 specularColor = Specular * texture(SpecularTex, tiledCoord);
    vec4 emissionColor = Emission * texture(EmissionTex, tiledCoord);

    LightingResult r = CalculateBlinnPhongLighting(i.Position, i.Normal, i.Depth);

    vec3 diffuse = r.Diffuse * diffuseColor.rgb;
    vec3 specular = r.Specular * specularColor.rgb;
    vec3 emission = emissionColor.a * emissionColor.rgb;

    FragColor = vec4(diffuse + specular + emission, 1);
}