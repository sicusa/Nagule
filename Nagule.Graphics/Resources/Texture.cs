namespace Nagule.Graphics;

using System.Numerics;

public record Texture : ResourceBase
{
    public static Texture Hint { get; } = new Texture {
        Image = Image.Hint,
        MinFilter = TextureMinFilter.Nearest,
        MaxFilter = TextureMagFilter.Nearest
    };
    public static Texture White { get; } = new Texture {
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