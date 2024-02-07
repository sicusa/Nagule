namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

public abstract class EffectManagerBase<TEffect> : GraphicsAssetManagerBase<TEffect, EffectMetadata>
    where TEffect : struct
{
    protected delegate Dyn ValueGetter(in TEffect asset);

    public abstract string Source { get; }
    public abstract string EntryPoint { get; }

    private readonly Dictionary<string, ValueGetter> _propGetters = [];

    protected void RegisterProperty<TPropertyCommand, TValue>(ValueGetter valueGetter)
        where TPropertyCommand : IPropertyCommand<TValue>, ICommand
    {
        _propGetters.Add(TPropertyCommand.PropertyName, valueGetter);

        var propName = EntryPoint + "_" + TPropertyCommand.PropertyName;

        Listen((in EntityRef entity, in TPropertyCommand cmd) => {
            ref var effect = ref entity.Get<TEffect>();
            ref var meta = ref entity.GetState<EffectMetadata>();

            var value = valueGetter(effect);
            meta.Properties = meta.Properties.SetItem(TPropertyCommand.PropertyName, value);

            var pipelineEntity = entity.GetState<EffectMetadata>().PipelineEntity;
            if (pipelineEntity != null) {
                var matEntity = pipelineEntity.Value.GetState<EffectPipelineState>().MaterialEntity;
                matEntity.Material_SetProperty(propName, value);
            }
        });
    }

    public override void LoadAsset(in EntityRef entity, ref TEffect asset, EntityRef stateEntity)
    {
        ref var meta = ref stateEntity.Get<EffectMetadata>();
        meta.Source = Source;
        meta.EntryPoint = EntryPoint;

        var propsBuilder = ImmutableDictionary.CreateBuilder<string, Dyn>();
        foreach (var (propName, getter) in _propGetters) {
            propsBuilder[propName] = getter(asset);
        }
        meta.Properties = propsBuilder.ToImmutable();
    }
}