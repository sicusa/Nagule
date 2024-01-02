namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class PostProcessingModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EffectEnvironmentModule>());