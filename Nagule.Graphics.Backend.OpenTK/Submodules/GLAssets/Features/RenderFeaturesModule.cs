namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class RenderFeaturesModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<Camera3DModule>()
            .Add<Mesh3DModule>()
            .Add<Light3DModule>());