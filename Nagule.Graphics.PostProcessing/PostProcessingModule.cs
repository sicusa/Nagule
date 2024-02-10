namespace Nagule.Graphics.PostProcessing;

using Sia;

public class PostProcessingModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EffectPipelineModule>()
            .Add<GLPostProcessingModule>()
            .Add<GammaCorrectionModule>()
            .Add<ACESToneMappingModule>()
            .Add<BloomModule>()
            .Add<BrightnessModule>()
            .Add<CubemapSkyboxModule>()
            .Add<ProcedualSkyboxModule>()
            .Add<ScreenSpaceAmbientOcclusionModule>()
            .Add<FastApproximateAntiAliasingModule>()
            .Add<DepthOfFieldModule>());