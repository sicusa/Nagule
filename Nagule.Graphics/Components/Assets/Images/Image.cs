namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

public record ImageAsset<TPixel> : ImageAssetBase
{
    public ImmutableArray<TPixel> Data { get; init; } = ImmutableArray<TPixel>.Empty;
}

[SiaTemplate(nameof(Image))]
[NaguleAsset<Image>]
public record ImageAsset : ImageAsset<byte>
{
    public static ImageAsset Hint { get; } = new() {
        PixelFormat = PixelFormat.RedGreenBlue,
        Data = [255, 0, 255],
        Width = 1,
        Height = 1
    };

    public static ImageAsset White { get; } = new() {
        PixelFormat = PixelFormat.RedGreenBlue,
        Data = [255, 255, 255],
        Width = 1,
        Height = 1
    };

    public static ImageAsset Load(string path)
        => ImageUtils.Load(File.OpenRead(path));

    public static ImageAsset Load(byte[] bytes, string name = "")
        => ImageUtils.Load(bytes, name);

    public static ImageAsset Load(Stream stream, string? name = null)
        => ImageUtils.Load(stream, name);
}