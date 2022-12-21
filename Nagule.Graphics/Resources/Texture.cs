namespace Nagule.Graphics;

using System.Numerics;

public enum TextureType
{
    Diffuse,
    Specular,
    Ambient,
    Emissive,
    Height,
    Normal,
    Opacity,
    Displacement,
    LightMap,
    Reflection,

    Unknown
}

public enum TextureWrapMode
{
    ClampToEdge,
    ClampToBorder,
    MirroredRepeat,
    Repeat
}

public enum TextureMinFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear
}

public enum TextureMagFilter
{
    Nearest,
    Linear
}

public record Texture : ResourceBase
{
    public static readonly Texture Hint = new Texture {
        Image = Image.Hint,
        MinFilter = TextureMinFilter.Nearest,
        MaxFilter = TextureMagFilter.Nearest
    };
    public static readonly Texture White = new Texture {
        Image = Image.White,
        MinFilter = TextureMinFilter.Nearest,
        MaxFilter = TextureMagFilter.Nearest
    };

    public Image? Image { get; init; }
    public TextureType Type { get; init; } = TextureType.Unknown;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;

    public TextureMinFilter MinFilter { get; init; } = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MaxFilter { get; init; } = TextureMagFilter.Linear;

    public Vector4 BorderColor { get; init; } = Vector4.Zero;
    public bool MipmapEnabled { get; init; } = true;
}