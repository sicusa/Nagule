namespace Nagule.Graphics.Backend.OpenTK;

public struct RenderSettingsData : IPooledComponent
{
    public IRenderPipeline RenderPipeline;
    public bool IsCompositionEnabled;
    public Guid? SkyboxId;

}