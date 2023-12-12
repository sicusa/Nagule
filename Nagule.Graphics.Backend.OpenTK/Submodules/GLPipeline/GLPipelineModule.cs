namespace Nagule.Graphics.Backend.OpenTK; 

using Sia;

[AfterSystem<GLInstancedModule>]
public class GLPipelineModule : AddonSystemBase
{
    private static readonly SystemChain StandardPipeline =
        SystemChain.Empty
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
            .Add<BlitColorToDisplayPass>()
            .Add<SwapBuffersPass>()
            .Add<FrameFinishPass>();

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var manager = AddAddon<Camera3DPipelineManager>(world);
        manager.PipelineChain = StandardPipeline;
    }
}