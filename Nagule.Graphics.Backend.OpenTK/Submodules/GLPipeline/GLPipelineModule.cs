namespace Nagule.Graphics.Backend.OpenTK; 

using Sia;

[AfterSystem<GLInstancedModule>]
public class GLPipelineModule : AddonSystemBase
{
    public sealed class StandardPipelineProvider : IRenderPipelineProvider
    {
        public static readonly StandardPipelineProvider Instance = new();
        private StandardPipelineProvider() {}

        public SystemChain TransformPipeline(in EntityRef entity, SystemChain chain)
            => chain
                .Add<FrameBeginPass>()
                .Add<Light3DCullingPass>()
                .Add<FrustumCullingPass>()
                .Add<DrawDepthCulledPass>(() => new() { MaximumInstanceCount = 256 })
                .Add<HierarchicalZBufferGeneratePass>()
                .Add<HierarchicalZCullingPass>()
                .Add<ActivateDefaultTexturesPass>()
                .Add<Light3DBuffersActivatePass>()
                .Add<DrawOpaqueCulledPass>(() => new() { MinimumInstanceCount = 257, DepthMask = false })
                .Add<DrawOpaqueCulledPass>(() => new() { MaximumInstanceCount = 256, DepthMask = true })
                .Add<DrawSkyboxPass>()
                .Add<DrawTransparentWBOITCulledPass>()
                .Add<PostProcessingBeginPass>()
                .Add<PostProcessingFinishPass>()
                .Add<BlitColorToDisplayPass>()
                .Add<SwapBuffersPass>()
                .Add<FrameFinishPass>();
    }
}