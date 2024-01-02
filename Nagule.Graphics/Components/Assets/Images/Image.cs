namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

public record RImage<TPixel> : RImageBase
{
    public ImmutableArray<TPixel> Data { get; init; } = [];
}

[SiaTemplate(nameof(Image))]
[NaAsset<Image>]
public record RImage : RImage<byte>
{
    public static RImage Hint { get; } = new() {
        PixelFormat = PixelFormat.RedGreenBlue,
        Data = [255, 0, 255],
        Width = 1,
        Height = 1
    };

    public static RImage White { get; } = new() {
        PixelFormat = PixelFormat.RedGreenBlue,
        Data = [255, 255, 255],
        Width = 1,
        Height = 1
    };

    public static RImage Load(string path)
        => ImageUtils.Load(File.OpenRead(path));

    public static RImage Load(byte[] bytes, string name = "")
        => ImageUtils.Load(bytes, name);

    public static RImage Load(Stream stream, string? name = null)
        => ImageUtils.Load(stream, name);
}