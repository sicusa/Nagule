namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class GLPostProcessingModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EffectEnvironmentModule>());