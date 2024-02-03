namespace Nagule.Graphics.Backends.OpenTK; 

using Sia;

[AfterSystem<GLInstancedModule>]
public class GLPipelineModule : AddonSystemBase
{
    public sealed class StandardPipelineProvider(bool depthOcclusionEnabled)
        : IRenderPipelineProvider
    {
        public RenderPassChain TransformPipeline(in EntityRef entity, RenderPassChain chain)
        {
            chain = chain
                .Add<FrameBeginPass>()
                .Add<Light3DClustererStartPass>();
            
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

            chain = chain
                .Add<DefaultTexturesActivatePass>()
                .Add<Light3DTexturesActivatePass>()

                .Add<Light3DClustererWaitForCompletionPass>()

                .Add<StageOpaqueBeginPass>()
                .Add<DrawOpaquePass>(
                    () => new() {
                        Cull = true,
                        GroupPredicate = GroupPredicates.Any,
                        MaterialPredicate = MaterialPredicates.IsOpaqueOrCutoff,
                        UseDrawnDepth = true
                    })
                .Add<StageOpaqueFinishPass>()
                
                .Add<StageBlendingBeginPass>()
                .Add<DrawBlendingPass>(() => new() { Cull = true })
                .Add<StageBlendingFinishPass>()

                .Add<StageTransparentBeginPass>()
                .Add<DrawTransparentWBOITPass>(() => new() { Cull = true })
                .Add<StageTransparentFinishPass>()

                .Add<StageUIBeginPass>()
                .Add<StageUIFinishPass>()

                .Add<StagePostProcessingBeginPass>()
                .Add<StagePostProcessingFinishPass>()

                .Add<BlitColorToTargetPass>()
                .Add<SwapBuffersPass>()
                .Add<FrameFinishPass>();
            
            return chain;
        }
    }
}