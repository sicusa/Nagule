namespace Nagule.Graphics.Backend.OpenTK;

public struct RenderSettingsData : IPooledComponent
{
    public IRenderPipeline RenderPipeline;
    public ICompositionPipeline? CompositionPipeline;
    public Guid? SkyboxId;

}