namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

public abstract class EffectManagerBase<TEffect, TEffectRecord>
    : GraphicsAssetManager<TEffect, TEffectRecord, EffectMetadata>
    where TEffect : struct, IAsset<TEffectRecord>, IConstructable<TEffect, TEffectRecord>
    where TEffectRecord : IAssetRecord
{
    protected delegate Dyn ValueGetter(in TEffect asset);

    public abstract string Source { get; }
    public abstract string EntryPoint { get; }

    private readonly Dictionary<string, ValueGetter> _propGetters = [];

    protected void RegisterProperty<TPropertyCommand, TValue>(ValueGetter valueGetter)
        where TPropertyCommand : IPropertyCommand<TValue>, ICommand
    {
        _propGetters.Add(TPropertyCommand.PropertyName, valueGetter);

        Listen((in EntityRef entity, in TPropertyCommand cmd) => {
            ref var effect = ref entity.Get<TEffect>();
            var value = valueGetter(effect);
            entity.EffectMetadata_SetProperty(TPropertyCommand.PropertyName, value);

            var pipelineEntity = entity.GetState<EffectMetadata>().PipelineEntity;
            if (pipelineEntity != null) {
                var matEntity = pipelineEntity.Value.GetState<EffectPipelineState>().MaterialEntity;
                matEntity.Material_SetProperty("EntryPoint" + TPropertyCommand.PropertyName, value);
            }
        });
    }

    protected override void LoadAsset(EntityRef entity, ref TEffect asset, EntityRef stateEntity)
    {
        ref var meta = ref stateEntity.Get<EffectMetadata>();
        meta.Source = Source;
        meta.EntryPoint = EntryPoint;

        var propsBuilder = ImmutableDictionary.CreateBuilder<string, Dyn>();
        foreach (var (propName, getter) in _propGetters) {
            propsBuilder[propName] = getter(asset);
        }
        stateEntity.Modify(ref meta, new EffectMetadata.SetProperties(propsBuilder.ToImmutable()));
    }
}