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
                .Add<FrustumCullingPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsOccluder,
                        MaterialPredicate = MaterialPredicates.IsOpaque
                    })

                .Add<StageDepthBeginPass>()
                .Add<DrawDepthCulledPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsOccluder,
                        MaterialPredicate = MaterialPredicates.IsOpaque
                    })
                .Add<StageDepthFinishPass>()

                .Add<HierarchicalZBufferGeneratePass>()
                .Add<HierarchicalZCullingPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsNonOccluder
                    })
                .Add<ActivateDefaultTexturesPass>()
                .Add<Light3DBuffersActivatePass>()

                .Add<StageOpaqueBeginPass>()
                .Add<DrawOpaqueCulledPass>(
                    () => new() {
                        GroupPredicate = GroupPredicates.IsNonOccluder,
                        MaterialPredicate = (in MaterialState s) =>
                            s.RenderMode == RenderMode.Opaque || s.RenderMode == RenderMode.Cutoff
                    })
                .Add<DrawOpaqueCulledPass>(() => new() {
                    GroupPredicate = GroupPredicates.IsOccluder,
                    DepthMask = false
                })
                .Add<StageOpaqueFinishPass>()

                .Add<StageSkyboxBeginPass>()
                .Add<DrawSkyboxPass>()
                .Add<StageSkyboxFinishPass>()
                
                .Add<StageBlendingBeginPass>()
                .Add<DrawBlendingCulledPass>()
                .Add<StageBlendingFinishPass>()

                .Add<StageTransparentBeginPass>()
                .Add<DrawTransparentWBOITCulledPass>()
                .Add<StageTransparentFinishPass>()

                .Add<StageUIBeginPass>()
                .Add<StageUIFinishPass>()

                .Add<StagePostProcessingBeginPass>()
                .Add<StagePostProcessingFinishPass>()

                .Add<BlitColorToDisplayPass>()
                .Add<SwapBuffersPass>()
                .Add<FrameFinishPass>();
    }
}