namespace Nagule.Graphics.PostProcessing;

using Sia;

public class PostProcessingModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EffectPipelineModule>()
            .Add<GammaCorrectionModule>()
            .Add<ACESToneMappingModule>()
            .Add<BloomModule>()
            .Add<BrightnessModule>()
            .Add<CubemapSkyboxModule>()
            .Add<ProcedualSkyboxModule>());