namespace Nagule.Graphics;

using System.Collections.Immutable;

public record ImageResource : ResourceBase
{
    public static readonly ImageResource Hint = new() {
        Bytes = ImmutableArray.Create<byte>(255, 255, 0, 255),
        Width = 1,
        Height = 1
    };
    public static readonly ImageResource White = new() {
        Bytes = ImmutableArray.Create<byte>(255, 255, 255, 255),
        Width = 1,
        Height = 1
    };

    public ImmutableArray<byte> Bytes { get; init; } = ImmutableArray<byte>.Empty;
    public PixelFormat PixelFormat { get; init; } = PixelFormat.RedGreenBlueAlpha;

    public int Width { get; init; }
    public int Height { get; init; }
}