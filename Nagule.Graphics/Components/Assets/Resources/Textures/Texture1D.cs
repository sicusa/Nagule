namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Texture1D))]
[NaAsset]
public record RTexture1D : RTextureBase
{
    public static RTexture1D Hint { get; } = new RTexture1D {
        Image = RImage.Hint,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest,
        IsMipmapEnabled = false
    };
    public static RTexture1D White { get; } = new RTexture1D {
        Image = RImage.White,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest,
        IsMipmapEnabled = false
    };

    public RImageBase? Image { get; init; }

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}