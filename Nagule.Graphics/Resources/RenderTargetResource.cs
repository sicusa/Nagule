namespace Nagule.Graphics;

public record RenderTargetResource : ResourceBase
{
    public static readonly RenderTargetResource AutoResized = new() {
        AutoResizeByWindow = true
    };

    public int Width { get; init; }
    public int Height { get; init; }
    public bool AutoResizeByWindow { get; init; } = false;
}