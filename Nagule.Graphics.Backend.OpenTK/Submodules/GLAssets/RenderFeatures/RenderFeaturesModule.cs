namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class RenderFeaturesModule : SystemBase
{
    public RenderFeaturesModule()
    {
        Children = SystemChain.Empty
            .Add<Camera3DModule>()
            .Add<Mesh3DModule>()
            .Add<Light3DModule>();
    }
}