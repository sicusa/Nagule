namespace Nagule.Graphics;

using System.Numerics;
using System.Runtime.InteropServices;

using Aeco;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct MaterialParameters
{
    public const int MemorySize = 4 * 16 + 4 * 4 + 2 * 8;

    public Vector4 DiffuseColor = Vector4.One;
    public Vector4 SpecularColor = Vector4.Zero;
    public Vector4 AmbientColor = Vector4.Zero;
    public Vector4 EmissiveColor = Vector4.Zero;
    public float Shininess = 32f;
    public float ShininessStrength = 1f;
    public float Reflectivity = 0f;
    public float Opacity = 1f;
    public Vector2 Tiling = Vector2.One;
    public Vector2 Offset = Vector2.Zero;
    
    public MaterialParameters() {}
}

public record MaterialResource : IResource
{
    public static readonly MaterialResource Default = new();
    public MaterialParameters Parameters = new();
    public readonly EnumArray<TextureType, TextureResource?> Textures = new();
    public bool IsTransparent = false;
}