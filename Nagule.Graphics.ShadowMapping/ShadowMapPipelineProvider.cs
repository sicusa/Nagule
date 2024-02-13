namespace Nagule.Graphics.ShadowMapping;

using Nagule.Graphics.Backends.OpenTK;

public class ShadowMapPipelineProvider : IRenderPipelineProvider
{
    public static readonly ShadowMapPipelineProvider Instance = new();
    private ShadowMapPipelineProvider() {}

    public RenderPassChain TransformPipeline(RenderPassChain chain, in RenderSettings settings)
    {
        chain = chain
            .Add<ShadowFrameBeginPass>();

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
                        MaterialPredicate = MaterialPredicates.Any
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
                        MaterialPredicate = MaterialPredicates.Any
                    })
                .Add<StageDepthFinishPass>();
        }
        
        chain = chain
            .Add<ShadowFrameFinishPass>();
        return chain;
    }
}