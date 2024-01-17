namespace Nagule.Graphics;

public abstract record RImageBase : AssetBase
{
    public PixelFormat PixelFormat { get; init; } = PixelFormat.RedGreenBlueAlpha;
    public abstract int Length { get; }

    public int Width { get; init; }
    public int Height { get; init; }

    public abstract ReadOnlySpan<byte> AsByteSpan();
}