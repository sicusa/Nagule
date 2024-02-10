namespace Nagule.Graphics.ShadowMapping;

using Nagule.Graphics.Backends.OpenTK;
using Sia;

public class ShadowMapPipelineProvider(bool depthOcclusionEnabled)
    : IRenderPipelineProvider
{
    public RenderPassChain TransformPipeline(RenderPassChain chain)
    {
        chain = chain
            .Add<FrameBeginPass>();

        if (depthOcclusionEnabled) {
            chain = chain
                .Add<FrustumCullingPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsOccluder,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff
                    })
                .Add<DrawDepthPass>(
                    () => new() {
                        Cull = true,
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
                        Cull = true,
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
                        Cull = true,
                        GroupPredicate = GroupPredicates.Any,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff
                    })
                .Add<StageDepthFinishPass>();
        }
        
        return chain;
    }
}