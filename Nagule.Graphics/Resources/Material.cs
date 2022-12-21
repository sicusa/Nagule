namespace Nagule.Graphics;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Immutable;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct MaterialParameters
{
    public const int MemorySize = 4 * 16 + 2 * 4 + 2 * 8;

    public Vector4 DiffuseColor = Vector4.One;
    public Vector4 SpecularColor = Vector4.Zero;
    public Vector4 AmbientColor = Vector4.Zero;
    public Vector4 EmissiveColor = Vector4.Zero;
    public float Shininess = 1f;
    public float Reflectivity = 0f;
    public Vector2 Tiling = Vector2.One;
    public Vector2 Offset = Vector2.Zero;
 
    public MaterialParameters() {}
}

public record Material : ResourceBase
{
    public static readonly Material Default = new();

    public string Name { get; set; } = "";
    public RenderMode RenderMode { get; init; } = RenderMode.Opaque;
    public bool IsTwoSided { get; init; }
    public MaterialParameters Parameters { get; init; } = new();
    public ShaderProgram? ShaderProgram { get; init; }

    public ImmutableDictionary<TextureType, Texture> Textures { get; init; } =
        ImmutableDictionary<TextureType, Texture>.Empty;

    public ImmutableDictionary<string, object> CustomParameters { get; init; } =
        ImmutableDictionary<string, object>.Empty;
    
    public Material WithTexture(TextureType type, Texture resource)
        => this with { Textures = Textures.SetItem(type, resource) };
    public Material WithTextures(params KeyValuePair<TextureType, Texture>[] textures)
        => this with { Textures = Textures.SetItems(textures) };
    public Material WithTextures(IEnumerable<KeyValuePair<TextureType, Texture>> textures)
        => this with { Textures = Textures.SetItems(textures) };

    public Material WithParameter(string name, object value)
        => this with { CustomParameters = CustomParameters.SetItem(name, value) };
    public Material WithParameters(params KeyValuePair<string, object>[] parameters)
        => this with { CustomParameters = CustomParameters.SetItems(parameters) };
    public Material WithParameters(IEnumerable<KeyValuePair<string, object>> parameters)
        => this with { CustomParameters = CustomParameters.SetItems(parameters) };
}