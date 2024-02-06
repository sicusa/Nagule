namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Tileset2D))]
[NaAsset]
public record RTileset2D : RTextureBase
{
    public static RTileset2D Empty { get; } = new();

    public RTileset2D()
    {
        MagFilter = TextureMagFilter.Nearest;
    }

    public RImageBase Image { get; init; } = RImage.Hint;
    public int TileWidth { get; init; }
    public int TileHeight { get; init; }
    public int? Count { get; init; }

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}