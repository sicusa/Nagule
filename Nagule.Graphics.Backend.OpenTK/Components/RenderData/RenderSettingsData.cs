namespace Nagule.Graphics.Backend.OpenTK;

public struct RenderSettingsData : IHashComponent
{
    public IRenderPipeline RenderPipeline;
    public ICompositionPipeline? CompositionPipeline;

    public Guid LightingEnvironmentId;
    public Guid? SkyboxId;
}