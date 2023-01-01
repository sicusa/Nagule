namespace Nagule.Graphics;

using System.Numerics;

public record RenderTexture : ResourceBase
{
    public static readonly RenderTexture AutoResized = new() {
        AutoResizeByWindow = true
    };

    public int Width { get; init; }
    public int Height { get; init; }
    public bool AutoResizeByWindow { get; init; } = false;

    public PixelFormat PixelFormat { get; init; } = PixelFormat.RedGreenBlueAlpha;
    public TextureType Type { get; init; } = TextureType.Unknown;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;

    public TextureMinFilter MinFilter { get; init; } = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MaxFilter { get; init; } = TextureMagFilter.Linear;

    public Vector4 BorderColor { get; init; } = Vector4.Zero;
    public bool MipmapEnabled { get; init; } = true;
}