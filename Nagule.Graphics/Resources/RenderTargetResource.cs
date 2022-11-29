namespace Nagule.Graphics;

public record RenderTargetResource : IResource
{
    public static readonly RenderTargetResource AutoResized = new() {
        AutoResizeByWindow = true
    };

    public int Width;
    public int Height;
    public bool AutoResizeByWindow = false;
}