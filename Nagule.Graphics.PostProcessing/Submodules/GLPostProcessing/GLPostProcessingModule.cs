namespace Nagule.Graphics.PostProcessing;

using Sia;

internal class GLPostProcessingModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EffectLayerModule>());