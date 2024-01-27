namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

internal class GLPostProcessingModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EffectLayerModule>());