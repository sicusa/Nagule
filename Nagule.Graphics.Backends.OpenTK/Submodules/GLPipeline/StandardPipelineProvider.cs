namespace Nagule.Graphics.Backends.OpenTK;

public sealed class StandardPipelineProvider : IRenderPipelineProvider
{
    public static readonly StandardPipelineProvider Instance = new();
    private StandardPipelineProvider() {}

    public RenderPassChain TransformPipeline(RenderPassChain chain, in RenderSettings settings)
    {
        chain = chain
            .Add<FrameBeginPass>()
            .Add<Light3DClustererStartPass>();
        
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

            .Add<BlitColorToRenderTargetPass>()

            .Add<LockBufferUpdatorPass<Mesh3DInstanceUpdator>>()
            .Add<LockBufferUpdatorPass<Camera3DUpdator>>()
            .Add<LockBufferUpdatorPass<Light3DUpdator>>()

            .Add<FrameFinishPass>();
        
        return chain;
    }
}