namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Texture3D))]
[NaAsset]
public record RTexture3D : RTextureBase
{
    public static RTexture3D Hint { get; } = new RTexture3D {
        Image = RImage.Hint,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest
    };
    public static RTexture3D White { get; } = new RTexture3D {
        Image = RImage.White,
        MinFilter = TextureMinFilter.Nearest,
        MagFilter = TextureMagFilter.Nearest
    };

    public RImageBase? Image { get; init; }

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}