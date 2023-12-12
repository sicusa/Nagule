namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(RenderTexture2D))]
[NaguleAsset<RenderTexture2D>]
public record RenderTexture2DAsset : TextureAssetBase
{
    public static RenderTexture2DAsset AutoResized { get; }
        = new() { AutoResizeByWindow = true };

    public int Width { get; init; }
    public int Height { get; init; }
    public bool AutoResizeByWindow { get; init; } = true;

    public PixelFormat PixelFormat { get; init; } = PixelFormat.RedGreenBlueAlpha;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
}