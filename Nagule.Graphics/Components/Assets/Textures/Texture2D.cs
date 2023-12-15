namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Texture2D))]
[NaguleAsset<Texture2D>]
public record Texture2DAsset : TextureAssetBase
{
    public static Texture2DAsset Hint { get; } = new Texture2DAsset {
        Image = ImageAsset.Hint,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest
    };
    public static Texture2DAsset White { get; } = new Texture2DAsset {
        Image = ImageAsset.White,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest
    };

    public ImageAssetBase? Image { get; init; }

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}