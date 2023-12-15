namespace Nagule.Graphics;

using System.Numerics;

public abstract record TextureAssetBase : AssetBase
{
    public TextureType Type { get; init; } = TextureType.Color;

    public TextureMinFilter MinFilter { get; init; } = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MagFilter { get; init; } = TextureMagFilter.Linear;

    public Vector4 BorderColor { get; init; } = Vector4.Zero;
    public bool MipmapEnabled { get; init; } = true;
}