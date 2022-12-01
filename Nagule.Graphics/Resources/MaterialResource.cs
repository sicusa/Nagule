namespace Nagule.Graphics;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Immutable;

using Aeco;

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

public record MaterialResource : IResource
{
    public static readonly MaterialResource Default = new();

    public string? Name;
    public MaterialParameters Parameters;
    public bool IsTransparent;

    public ImmutableDictionary<TextureType, TextureResource> Textures =
        ImmutableDictionary<TextureType, TextureResource>.Empty;
    
    public MaterialResource WithTexture(TextureType type, TextureResource resource)
        => this with { Textures = Textures.SetItem(type, resource) };

    public MaterialResource WithTextures(IEnumerable<KeyValuePair<TextureType, TextureResource>> textures)
        => this with { Textures = Textures.SetItems(textures) };

    public MaterialResource WithoutTexture(TextureType type)
        => this with { Textures = Textures.Remove(type) };

    public MaterialResource WithoutTextures(IEnumerable<TextureType> types)
        => this with { Textures = Textures.RemoveRange(types) };
}