namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class EffectLayerManager
{
    private class DrawEffectsPassProvider(EntityRef pipelineEntity) : IRenderPipelineProvider
    {
        public RenderPassChain TransformPipeline(RenderPassChain chain, in RenderSettings settings)
            => chain.Add<DrawEffectsPass>(() => new(pipelineEntity));
    }

    public override void LoadAsset(in EntityRef entity, ref EffectLayer asset, EntityRef stateEntity)
    {
        var pipeline = asset.Pipeline;
        if (pipeline.Effects.Count == 0) {
            return;
        }

        ref var provider = ref stateEntity.Get<RenderPipelineProvider>();
        var pipelineEntity = World.AcquireAsset(pipeline, entity);
        provider.Instance = new DrawEffectsPassProvider(pipelineEntity);

        RenderFramer.Start(() => {
            ref var state = ref stateEntity.Get<EffectLayerState>();
            state.PipelineEntity = pipelineEntity;
            return true;
        });
    }
}