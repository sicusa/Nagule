namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public static class MaterialKeys
{
    // uv

    public static readonly TypedKey<Vector2> Tiling = nameof(Tiling);
    public static readonly TypedKey<Vector2> Offset = nameof(Offset);

    // emission

    public static readonly TypedKey<Vector4> Emission = nameof(Emission);
    public static readonly TypedKey<Texture> EmissionTex = nameof(EmissionTex);

    // lighting

    public static readonly TypedKey<Texture> NormalTex = nameof(NormalTex);
    public static readonly TypedKey<Texture> LightmapTex = nameof(LightmapTex);
    public static readonly TypedKey<Texture> AmbientOcclusionTex = nameof(AmbientOcclusionTex);
    public static readonly TypedKey<float> AmbientOcclusionMultiplier = nameof(AmbientOcclusionMultiplier);

    // transparency

    public static readonly TypedKey<Texture> OpacityTex = nameof(OpacityTex);

    // cutoff

    public static readonly TypedKey<float> Threshold = nameof(Threshold);

    // reflection

    public static readonly TypedKey<float> Reflectivity = nameof(Reflectivity);
    public static readonly TypedKey<Texture> ReflectionTex = nameof(ReflectionTex);

    // parallax mapping

    public static readonly TypedKey<Texture> HeightTex = nameof(HeightTex);
    public static readonly TypedKey<float> ParallaxScale = nameof(ParallaxScale);
    public static readonly TypedKey<int> ParallaxMinimumLayerCount = nameof(ParallaxMinimumLayerCount);
    public static readonly TypedKey<int> ParallaxMaximumLayerCount = nameof(ParallaxMaximumLayerCount);

    public static readonly TypedKey<Dyn.Unit> EnableParallaxEdgeClip = nameof(EnableParallaxEdgeClip);
    public static readonly TypedKey<Dyn.Unit> EnableParallaxShadow = nameof(EnableParallaxShadow);

    // displacement mapping

    public static readonly TypedKey<Texture> DisplacementTex = nameof(DisplacementTex);

    // blinn-phong

    public static readonly TypedKey<Vector4> Diffuse = nameof(Diffuse);
    public static readonly TypedKey<Texture> DiffuseTex = nameof(DiffuseTex);
    public static readonly TypedKey<Vector4> Specular = nameof(Specular);
    public static readonly TypedKey<Texture> SpecularTex = nameof(SpecularTex);
    public static readonly TypedKey<Vector4> Ambient = nameof(Ambient);
    public static readonly TypedKey<Texture> AmbientTex = nameof(AmbientTex);
    public static readonly TypedKey<float> Shininess = nameof(Shininess);

    // physically based rendering

    public static readonly TypedKey<Vector4> Albedo = nameof(Diffuse);
    public static readonly TypedKey<Vector4> Metallic = nameof(Metallic);
    public static readonly TypedKey<Vector4> Roughness = nameof(Roughness);

    public static readonly TypedKey<Texture> AlbedoTex = nameof(DiffuseTex);
    public static readonly TypedKey<Texture> MetallicTex = nameof(MetallicTex);
    public static readonly TypedKey<Texture> RoughnessTex = nameof(RoughnessTex);

    // environment

    public static readonly TypedKey<Texture> SkyboxTex = nameof(SkyboxTex);
}

public record Material : ResourceBase
{
    public static Material Default { get; } = new();

    public RenderMode RenderMode { get; init; } = RenderMode.Opaque;
    public LightingMode LightingMode { get; init; } = LightingMode.Full;
    public bool IsTwoSided { get; init; }
    public GLSLProgram? ShaderProgram { get; init; }

    public ImmutableDictionary<string, Dyn> Properties { get; init; } =
        ImmutableDictionary<string, Dyn>.Empty;

    public Material WithProperty(string name, Dyn value)
        => this with { Properties = Properties.SetItem(name, value) };
    public Material WithProperty(MaterialProperty property)
        => this with { Properties = Properties.SetItem(property.Name, property.Value) };
    public Material WithProperties(params MaterialProperty[] properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
    public Material WithProperties(IEnumerable<MaterialProperty> properties)
        => this with { Properties = Properties.SetItems(properties.Select(MaterialProperty.ToPair)) };
}