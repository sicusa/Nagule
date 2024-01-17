namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(RenderTexture2D))]
[NaAsset]
public record RRenderTexture2D : RTextureBase
{
    public static RRenderTexture2D Screen { get; }
        = new() {
            Type = TextureType.Color,
            Image = new RImage {
                PixelFormat = PixelFormat.RedGreenBlue
            },
            AutoResizeByWindow = true
        };

    public RImageBase? Image { get; init; }
    public bool AutoResizeByWindow { get; init; } = true;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}