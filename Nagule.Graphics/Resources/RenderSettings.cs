namespace Nagule.Graphics;

public record RenderSettings : ResourceBase
{
    public static RenderSettings Default { get; } = new();

    public int Width { get; }
    public int Height { get; }
    public bool AutoResizeByWindow { get; } = true;

    public RenderPipeline RenderPipeline { get; init; }
        = RenderPipeline.Default;
    public CompositionPipeline? CompositionPipeline { get; init; }
        = CompositionPipeline.Default;

    public Cubemap? Skybox { get; init; }
}