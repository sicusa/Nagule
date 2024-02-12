namespace Nagule.Graphics.Backends.OpenTK;

public sealed class StandardPipelineProvider : IRenderPipelineProvider
{
    public static readonly StandardPipelineProvider Instance = new();
    private StandardPipelineProvider() {}

    public RenderPassChain TransformPipeline(RenderPassChain chain, in RenderSettings settings)
    {
        chain = chain
            .Add<FrameBeginPass>()
            .Add<Light3DClustererStartPass>()
            .Add<WaitForGraphicsUpdatorPass<Mesh3DInstanceUpdator>>()
            .Add<WaitForGraphicsUpdatorPass<Camera3DUpdator>>();
        
        if (settings.IsOcclusionCullingEnabled) {
            chain = chain
                .Add<FrustumCullingPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsOccluder,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff
                    })
                .Add<DrawDepthPass>(
                    () => new() {
                        IsCulled = true,
                        GroupPredicate = GroupPredicates.IsOccluder,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff
                    })

                .Add<HierarchicalZBufferGeneratePass>()
                .Add<HierarchicalZCullingPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsNonOccluder,
                        MaterialPredicate = MaterialPredicates.Any
                    })

                .Add<StageDepthBeginPass>()
                .Add<DrawDepthPass>(
                    () => new() {
                        IsCulled = true,
                        GroupPredicate = GroupPredicates.IsNonOccluder,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff
                    })
                .Add<StageDepthFinishPass>();
        }
        else {
            chain = chain
                .Add<FrustumCullingPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.Any,
                        MaterialPredicate = MaterialPredicates.Any
                    })

                .Add<StageDepthBeginPass>()
                .Add<DrawDepthPass>(
                    () => new() {
                        IsCulled = true,
                        GroupPredicate = GroupPredicates.Any,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff
                    })
                .Add<StageDepthFinishPass>();
        }

        chain = chain
            .Add<DefaultTexturesActivatePass>()
            .Add<Light3DTexturesActivatePass>()
            .Add<Light3DClustererWaitForCompletionPass>()
            .Add<WaitForGraphicsUpdatorPass<Light3DUpdator>>()

            .Add<StageOpaqueBeginPass>()
            .Add<DrawOpaquePass>(
                () => new() {
                    IsCulled = true,
                    GroupPredicate = GroupPredicates.Any,
                    MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff,
                    UseDrawnDepth = true
                })
            .Add<StageOpaqueFinishPass>()
            
            .Add<StageBlendingBeginPass>()
            .Add<DrawBlendingPass>(() => new() { IsCulled = true })
            .Add<StageBlendingFinishPass>()

            .Add<StageTransparentBeginPass>()
            .Add<DrawTransparentWBOITPass>(() => new() { IsCulled = true })
            .Add<StageTransparentFinishPass>()

            .Add<StageUIBeginPass>()
            .Add<StageUIFinishPass>()

            .Add<StagePostProcessingBeginPass>()
            .Add<StagePostProcessingFinishPass>()

            .Add<FrameFinishPass>()
            .Add<BlitColorToRenderTargetPass>();
        
        return chain;
    }
}