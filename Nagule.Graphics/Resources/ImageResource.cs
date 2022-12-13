namespace Nagule.Graphics;

public record ImageResource : ResourceBase
{
    public static readonly ImageResource Hint = new() {
        Bytes = new byte[] {255, 255, 0, 255},
        Width = 1,
        Height = 1
    };
    public static readonly ImageResource White = new() {
        Bytes = new byte[] {255, 255, 255, 255},
        Width = 1,
        Height = 1
    };

    public byte[]? Bytes;
    public PixelFormat PixelFormat = PixelFormat.RedGreenBlueAlpha;

    public int Width;
    public int Height;
}