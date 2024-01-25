namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(HDRImage))]
[NaAsset]
public record RHDRImage : RImage<float>, ILoadableAssetRecord<RHDRImage>
{
    public static RHDRImage Empty { get; } = new() {
        PixelFormat = PixelFormat.RGB,
        Data = [],
        Width = 1,
        Height = 1
    };

    public static RHDRImage Hint { get; } = new() {
        PixelFormat = PixelFormat.RGB,
        Data = [1f, 0, 1f],
        Width = 1,
        Height = 1
    };

    public static RHDRImage White { get; } = new() {
        PixelFormat = PixelFormat.RGB,
        Data = [1f, 1f, 1f],
        Width = 1,
        Height = 1
    };

    public static RHDRImage Load(byte[] bytes, string name = "")
        => ImageUtils.LoadHDR(bytes, name);

    public static RHDRImage Load(Stream stream, string? name = null)
        => ImageUtils.LoadHDR(stream, name);
}