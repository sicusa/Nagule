namespace Nagule.Graphics.Backends.OpenTK;

using Sia;
using Nagule.Graphics.PostProcessing;
using System.Diagnostics.CodeAnalysis;

public partial class EffectLayerManager
{
    private class DrawEffectsPassProvider(EntityRef pipelineEntity) : IRenderPipelineProvider
    {
        public RenderPassChain TransformPipeline(in EntityRef entity, RenderPassChain chain)
            => chain.Add<DrawEffectsPass>(() => new(pipelineEntity));
    }

    [AllowNull] public EffectPipelineManager _pipelineManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _pipelineManager = world.GetAddon<EffectPipelineManager>();
    }

    protected override void LoadAsset(EntityRef entity, ref EffectLayer asset, EntityRef stateEntity)
    {
        var pipeline = asset.Pipeline;
        if (pipeline.Effects.Count == 0) {
            return;
        }

        ref var provider = ref stateEntity.Get<RenderPipelineProvider>();
        var pipelineEntity = _pipelineManager.Acquire(pipeline, entity);
        provider.Instance = new DrawEffectsPassProvider(pipelineEntity);

        RenderFramer.Start(() => {
            ref var state = ref stateEntity.Get<EffectLayerState>();
            state.PipelineEntity = pipelineEntity;
            return true;
        });
    }
}