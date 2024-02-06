namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Texture2D))]
[NaAsset]
public record RTexture2D : RTextureBase
{
    public static RTexture2D Hint { get; } = new RTexture2D {
        Image = RImage.Hint,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest,
        MipmapEnabled = false
    };
    public static RTexture2D White { get; } = new RTexture2D {
        Image = RImage.White,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest,
        MipmapEnabled = false
    };

    public RImageBase Image { get; init; } = RImage.Hint;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}