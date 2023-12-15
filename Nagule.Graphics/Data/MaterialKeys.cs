namespace Nagule.Graphics;

using System.Numerics;

public static class MaterialKeys
{
    // uv

    public static readonly TypedKey<Vector2> Tiling = nameof(Tiling);
    public static readonly TypedKey<Vector2> Offset = nameof(Offset);

    // emission

    public static readonly TypedKey<Vector4> Emission = nameof(Emission);
    public static readonly TypedKey<Texture2DAsset> EmissionTex = nameof(EmissionTex);

    // lighting

    public static readonly TypedKey<Texture2DAsset> NormalTex = nameof(NormalTex);
    public static readonly TypedKey<Texture2DAsset> LightmapTex = nameof(LightmapTex);
    public static readonly TypedKey<Texture2DAsset> AmbientOcclusionTex = nameof(AmbientOcclusionTex);
    public static readonly TypedKey<float> AmbientOcclusionMultiplier = nameof(AmbientOcclusionMultiplier);

    // transparency

    public static readonly TypedKey<Texture2DAsset> OpacityTex = nameof(OpacityTex);

    // cutoff

    public static readonly TypedKey<float> Threshold = nameof(Threshold);

    // reflection

    public static readonly TypedKey<float> Reflectivity = nameof(Reflectivity);
    public static readonly TypedKey<CubemapAsset> ReflectionTex = nameof(ReflectionTex);

    // parallax mapping

    public static readonly TypedKey<Texture2DAsset> HeightTex = nameof(HeightTex);
    public static readonly TypedKey<float> ParallaxScale = nameof(ParallaxScale);
    public static readonly TypedKey<int> ParallaxMinimumLayerCount = nameof(ParallaxMinimumLayerCount);
    public static readonly TypedKey<int> ParallaxMaximumLayerCount = nameof(ParallaxMaximumLayerCount);

    public static readonly TypedKey<Dyn.Unit> EnableParallaxEdgeClip = nameof(EnableParallaxEdgeClip);
    public static readonly TypedKey<Dyn.Unit> EnableParallaxShadow = nameof(EnableParallaxShadow);

    // displacement mapping

    public static readonly TypedKey<Texture2DAsset> DisplacementTex = nameof(DisplacementTex);

    // blinn-phong

    public static readonly TypedKey<Vector4> Diffuse = nameof(Diffuse);
    public static readonly TypedKey<Texture2DAsset> DiffuseTex = nameof(DiffuseTex);
    public static readonly TypedKey<Vector4> Specular = nameof(Specular);
    public static readonly TypedKey<Texture2DAsset> SpecularTex = nameof(SpecularTex);
    public static readonly TypedKey<Vector4> Ambient = nameof(Ambient);
    public static readonly TypedKey<Texture2DAsset> AmbientTex = nameof(AmbientTex);
    public static readonly TypedKey<float> Shininess = nameof(Shininess);

    // physically based rendering

    public static readonly TypedKey<Vector4> Albedo = nameof(Albedo);
    public static readonly TypedKey<Vector4> Metallic = nameof(Metallic);
    public static readonly TypedKey<Vector4> Roughness = nameof(Roughness);

    public static readonly TypedKey<Texture2DAsset> AlbedoTex = nameof(AlbedoTex);
    public static readonly TypedKey<Texture2DAsset> MetallicTex = nameof(MetallicTex);
    public static readonly TypedKey<Texture2DAsset> RoughnessTex = nameof(RoughnessTex);

    // environment

    public static readonly TypedKey<Texture2DAsset> SkyboxTex = nameof(SkyboxTex);
}