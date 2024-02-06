namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

internal class FeaturesModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<Camera3DModule>()
            .Add<Mesh3DModule>()
            .Add<Light3DModule>());