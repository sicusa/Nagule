namespace Nagule.Graphics;

using System.Numerics;

public abstract record RTextureBase : AssetBase
{
    public TextureUsage Usage { get; init; } = TextureUsage.Color;

    public TextureMinFilter MinFilter { get; init; } = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MagFilter { get; init; } = TextureMagFilter.Linear;

    public Vector4 BorderColor { get; init; } = Vector4.Zero;
    public bool IsMipmapEnabled { get; init; } = true;
}