namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(RenderTexture2D))]
[NaAsset]
public record RRenderTexture2D : RTexture2D
{
    public static RRenderTexture2D Screen { get; }
        = new() {
            Usage = TextureUsage.Color,
            Image = new RImage<Half> {
                PixelFormat = PixelFormat.RGB
            },
            AutoResizeByWindow = true
        };

    public bool AutoResizeByWindow { get; init; } = false;

    public RRenderTexture2D()
    {
        MipmapEnabled = false;
        WrapU = TextureWrapMode.ClampToEdge;
        WrapV = TextureWrapMode.ClampToEdge;
    }
}