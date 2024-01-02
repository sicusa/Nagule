namespace Nagule.Graphics;

using System.Numerics;

public static class MaterialKeys
{
    // uv

    public static readonly TypedKey<Vector2> Tiling = nameof(Tiling);
    public static readonly TypedKey<Vector2> Offset = nameof(Offset);

    // emission

    public static readonly TypedKey<Vector4> Emission = nameof(Emission);
    public static readonly TypedKey<RTexture2D> EmissionTex = nameof(EmissionTex);

    // lighting

    public static readonly TypedKey<RTexture2D> NormalTex = nameof(NormalTex);
    public static readonly TypedKey<RTexture2D> LightmapTex = nameof(LightmapTex);
    public static readonly TypedKey<RTexture2D> OcclusionTex = nameof(OcclusionTex);
    public static readonly TypedKey<float> OcclusionStrength = nameof(OcclusionStrength);

    // transparency

    public static readonly TypedKey<RTexture2D> OpacityTex = nameof(OpacityTex);

    // cutoff

    public static readonly TypedKey<float> Threshold = nameof(Threshold);

    // reflection

    public static readonly TypedKey<float> Reflectivity = nameof(Reflectivity);
    public static readonly TypedKey<RCubemap> ReflectionTex = nameof(ReflectionTex);

    // parallax mapping

    public static readonly TypedKey<RTexture2D> HeightTex = nameof(HeightTex);
    public static readonly TypedKey<float> ParallaxScale = nameof(ParallaxScale);
    public static readonly TypedKey<int> ParallaxMinimumLayerCount = nameof(ParallaxMinimumLayerCount);
    public static readonly TypedKey<int> ParallaxMaximumLayerCount = nameof(ParallaxMaximumLayerCount);

    public static readonly TypedKey<Dyn.Unit> EnableParallaxEdgeClip = nameof(EnableParallaxEdgeClip);
    public static readonly TypedKey<Dyn.Unit> EnableParallaxShadow = nameof(EnableParallaxShadow);

    // displacement mapping

    public static readonly TypedKey<RTexture2D> DisplacementTex = nameof(DisplacementTex);

    // blinn-phong

    public static readonly TypedKey<Vector4> Diffuse = nameof(Diffuse);
    public static readonly TypedKey<RTexture2D> DiffuseTex = nameof(DiffuseTex);
    public static readonly TypedKey<Vector4> Specular = nameof(Specular);
    public static readonly TypedKey<RTexture2D> SpecularTex = nameof(SpecularTex);
    public static readonly TypedKey<Vector4> Ambient = nameof(Ambient);
    public static readonly TypedKey<RTexture2D> AmbientTex = nameof(AmbientTex);
    public static readonly TypedKey<float> Shininess = nameof(Shininess);

    // physically based rendering

    public static readonly TypedKey<Vector4> Albedo = nameof(Albedo);
    public static readonly TypedKey<Vector4> Metallic = nameof(Metallic);
    public static readonly TypedKey<Vector4> Roughness = nameof(Roughness);

    public static readonly TypedKey<RTexture2D> AlbedoTex = nameof(AlbedoTex);
    public static readonly TypedKey<RTexture2D> MetallicTex = nameof(MetallicTex);
    public static readonly TypedKey<RTexture2D> RoughnessTex = nameof(RoughnessTex);

    // environment

    public static readonly TypedKey<RTexture2D> SkyboxTex = nameof(SkyboxTex);
}