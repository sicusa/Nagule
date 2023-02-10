namespace Nagule.Graphics;

public record RenderSettings : ResourceBase
{
    public static RenderSettings Default { get; } = new();

    public int Width { get; }
    public int Height { get; }
    public bool AutoResizeByWindow { get; } = true;

    public RenderPipeline RenderPipeline { get; init; } = RenderPipeline.Default;
    public bool IsCompositionEnabled { get; init; } = true;

    public Cubemap? Skybox { get; init; }
}