#version 410 core

#include <nagule/blinn_phong.glsl>
#include <nagule/transparency.glsl>

uniform sampler2D DiffuseTex;
uniform sampler2D SpecularTex;
uniform sampler2D EmissionTex;

in VertexOutput {
    vec3 Position;
    vec2 TexCoord;
    vec3 Normal;
    float Depth;
} i;

OUT_ACCUM vec4 Accum;
OUT_REVEAL float Reveal;

void main()
{
    vec2 tiledCoord = i.TexCoord * Tiling + Offset;

    vec4 diffuseColor = Diffuse * texture(DiffuseTex, tiledCoord);
    vec4 specularColor = Specular * texture(SpecularTex, tiledCoord);
    vec4 emissionColor = Emission * texture(EmissionTex, tiledCoord);

    LightingResult r = CalculateBlinnPhongLighting(i.Position, i.Normal, i.Depth);

    vec3 diffuse = r.Diffuse * diffuseColor.rgb;
    vec3 specular = r.Specular * specularColor.rgb;
    vec3 emission = emissionColor.a * emissionColor.rgb;
    vec4 color = vec4(diffuse + specular + emission, diffuseColor.a);

    Reveal = GetTransparencyWeight(color) * color.a;
    Accum = vec4(color.rgb * Reveal, color.a);
}