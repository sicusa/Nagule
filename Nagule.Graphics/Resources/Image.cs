namespace Nagule.Graphics;

using System.Collections.Immutable;

public record Image : ResourceBase
{
    public static Image Hint { get; } = new() {
        Bytes = ImmutableArray.Create<byte>(255, 255, 0, 255),
        Width = 1,
        Height = 1
    };
    public static Image White { get; } = new() {
        Bytes = ImmutableArray.Create<byte>(255, 255, 255, 255),
        Width = 1,
        Height = 1
    };

    public ImmutableArray<byte> Bytes { get; init; } = ImmutableArray<byte>.Empty;
    public PixelFormat PixelFormat { get; init; } = PixelFormat.RedGreenBlueAlpha;

    public int Width { get; init; }
    public int Height { get; init; }
}