namespace Nagule.Graphics;

public struct RenderSettingsProps : IHashComponent
{
    public ReactiveObject<int> Width { get; } = new();
    public ReactiveObject<int> Height { get; } = new();
    public ReactiveObject<bool> AutoResizeByWindow { get; } = new();

    public ReactiveObject<Cubemap?> Skybox { get; } = new();

    public ReactiveObject<RenderPipeline> RenderPipeline { get; } = new();
    public ReactiveObject<CompositionPipeline?> CompositionPipeline { get; } = new();

    public RenderSettingsProps() {}

    public void Set(RenderSettings resource)
    {
        Width.Value = resource.Width;
        Height.Value = resource.Height;
        AutoResizeByWindow.Value = resource.AutoResizeByWindow;

        Skybox.Value = resource.Skybox;

        RenderPipeline.Value = resource.RenderPipeline;
        CompositionPipeline.Value = resource.CompositionPipeline;
    }
}

public record RenderSettings : ResourceBase<RenderSettingsProps>
{
    public static RenderSettings Default { get; } = new();

    public int Width { get; }
    public int Height { get; }
    public bool AutoResizeByWindow { get; } = true;

    public Cubemap? Skybox { get; init; }

    public RenderPipeline RenderPipeline { get; init; }
        = RenderPipeline.Default;
    public CompositionPipeline? CompositionPipeline { get; init; }
        = CompositionPipeline.Default;
}