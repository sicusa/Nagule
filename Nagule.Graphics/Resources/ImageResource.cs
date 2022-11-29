namespace Nagule.Graphics;

public record ImageResource : IResource
{
    public static readonly ImageResource Hint = new() {
        Bytes = new byte[] {255, 255, 0, 0},
        Width = 1,
        Height = 1
    };
    public static readonly ImageResource White = new() {
        Bytes = new byte[] {255, 255, 255, 255},
        Width = 1,
        Height = 1
    };

    public byte[]? Bytes;

    public int Width;
    public int Height;
}