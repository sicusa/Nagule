namespace Nagule.Graphics.Backend.OpenTK;

using Sia;
using Nagule.Graphics.PostProcessing;
using System.Diagnostics.CodeAnalysis;

public partial class EffectEnvironmentManager
{
    private class DrawEffectsPassProvider(EntityRef pipelineEntity) : IRenderPipelineProvider
    {
        public SystemChain TransformPipeline(in EntityRef entity, SystemChain chain)
            => chain.Add<DrawEffectsPass>(() => new(pipelineEntity));
    }

    [AllowNull] public EffectPipelineManager _pipelineManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _pipelineManager = world.GetAddon<EffectPipelineManager>();

        Listen((EntityRef entity, ref EffectEnvironment snapshot, in EffectEnvironment.SetPipeline cmd) => {
            if (_pipelineManager.TryGet(snapshot.Pipeline, out var prevPipelineEntity)) {
                entity.UnreferAsset(prevPipelineEntity);
            }
            LoadPipeline(entity, cmd.Value, entity.GetStateEntity());
        });
    }

    protected override void LoadAsset(EntityRef entity, ref EffectEnvironment asset, EntityRef stateEntity)
    {
        LoadPipeline(entity, asset.Pipeline, stateEntity);
    }

    private void LoadPipeline(EntityRef entity, REffectPipeline pipeline, EntityRef stateEntity)
    {
        ref var provider = ref stateEntity.Get<RenderPipelineProvider>();
        provider.Instance = null;

        if (pipeline.Effects.Count == 0) {
            return;
        }

        var pipelineEntity = _pipelineManager.Acquire(pipeline, entity);
        provider.Instance = new DrawEffectsPassProvider(pipelineEntity);

        RenderFrame.Start(() => {
            stateEntity.Get<EffectEnvironmentState>().PipelineEntity = pipelineEntity;
            return true;
        });
    }
}