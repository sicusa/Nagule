namespace Nagule.Graphics.Backend.OpenTK;

public struct RenderSettingsData : IHashComponent
{
    public bool AutoResizeByWindow;

    public IRenderPipeline RenderPipeline;
    public ICompositionPipeline? CompositionPipeline;

    public uint LightingEnvironmentId;
    public uint? SkyboxId;
}