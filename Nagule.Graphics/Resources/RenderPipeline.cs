namespace Nagule.Graphics;

public record RenderPipeline : ResourceBase
{
    public static readonly RenderPipeline AutoResized = new() {
        AutoResizeByWindow = true
    };

    public int Width { get; init; }
    public int Height { get; init; }
    public bool AutoResizeByWindow { get; init; } = false;
}