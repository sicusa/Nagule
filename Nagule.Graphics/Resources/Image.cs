namespace Nagule.Graphics;

using System.Collections.Immutable;

public abstract record ImageBase : ResourceBase
{
    public PixelFormat PixelFormat { get; init; } = PixelFormat.RedGreenBlueAlpha;

    public int Width { get; init; }
    public int Height { get; init; }
}

public record Image<TPixel> : ImageBase
{
    public ImmutableArray<TPixel> Data { get; init; } = ImmutableArray<TPixel>.Empty;
}

public record Image : Image<byte>
{
    public static Image Hint { get; } = new() {
        Data = ImmutableArray.Create<byte>(255, 0, 255, 255),
        Width = 1,
        Height = 1
    };

    public static Image White { get; } = new() {
        Data = ImmutableArray.Create<byte>(255, 255, 255, 255),
        Width = 1,
        Height = 1
    };
}