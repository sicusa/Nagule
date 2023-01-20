namespace Nagule.Graphics.Backend.OpenTK;

public class RenderSettingsManager : ResourceManagerBase<RenderSettings>
{
    protected override void Initialize(IContext context, Guid id, RenderSettings resource, RenderSettings? prevResource)
    {
        if (prevResource != null) {
            Uninitialize(context, id, resource);
        }
    }

    protected override void Uninitialize(IContext context, Guid id, RenderSettings resource)
    {
        if (resource.Skybox != null) {
            ResourceLibrary<Cubemap>.Unreference(context, id, resource.Skybox);
        }
    }
}