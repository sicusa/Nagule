namespace Nagule.Graphics;

public record RenderTarget : ResourceBase
{
    public static readonly RenderTarget AutoResized = new() {
        AutoResizeByWindow = true
    };

    public int Width { get; init; }
    public int Height { get; init; }
    public bool AutoResizeByWindow { get; init; } = false;
}