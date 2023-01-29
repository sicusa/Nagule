namespace Nagule.Graphics;

using System.Collections.Immutable;

public static class MaterialKeys
{
    public static readonly string Diffuse = "Diffuse";
    public static readonly string Specular = "Specular";
    public static readonly string Ambient = "Ambient";
    public static readonly string Emission = "Emission";
    public static readonly string Shininess = "Shininess";
    public static readonly string Reflectivity = "Reflectivity";
    public static readonly string Tiling = "Tiling";
    public static readonly string Offset = "Offset";
    public static readonly string Threshold = "Threshold";

    public static readonly string UITex = "UITex";
    public static readonly string DiffuseTex = "DiffuseTex";
    public static readonly string SpecularTex = "SpecularTex";
    public static readonly string AmbientTex = "AmbientTex";
    public static readonly string EmissionTex = "EmissionTex";
    public static readonly string HeightTex = "HeightTex";
    public static readonly string NormalTex = "NormalTex";
    public static readonly string OpacityTex = "OpacityTex";
    public static readonly string DisplacementTex = "DisplacementTex";
    public static readonly string LightmapTex = "LightmapTex";
    public static readonly string ReflectionTex = "ReflectionTex";
    public static readonly string AmbientOcclusionTex = "AmbientOcclusionTex";
    public static readonly string SkyboxTex = "SkyboxTex";
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

    public ImmutableDictionary<string, Texture> Textures { get; init; } =
        ImmutableDictionary<string, Texture>.Empty;

    public Material WithProperty(string name, Dyn value)
        => this with { Properties = Properties.SetItem(name, value) };
    public Material WithProperties(IEnumerable<KeyValuePair<string, Dyn>> properties)
        => this with { Properties = Properties.SetItems(properties) };
    public Material WithProperties(params Property[] properties)
        => this with { Properties = Properties.SetItems(properties.Select(Property.ToPair)) };
    public Material WithProperties(IEnumerable<Property> properties)
        => this with { Properties = Properties.SetItems(properties.Select(Property.ToPair)) };
    
    public Material WithTexture(string name, Texture texture)
        => this with { Textures = Textures.SetItem(name, texture) };
    public Material WithTextures(params KeyValuePair<string, Texture>[] textures)
        => this with { Textures = Textures.SetItems(textures) };
    public Material WithTextures(IEnumerable<KeyValuePair<string, Texture>> textures)
        => this with { Textures = Textures.SetItems(textures) };
}