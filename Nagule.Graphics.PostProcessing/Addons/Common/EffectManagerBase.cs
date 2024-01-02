namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

public abstract class EffectManagerBase<TEffect, TEffectAsset>
    : GraphicsAssetManagerBase<TEffect, TEffectAsset, EffectMetadata>
    where TEffect : struct, IAsset<TEffectAsset>, IConstructable<TEffect, TEffectAsset>
    where TEffectAsset : IAsset
{
    protected delegate Dyn ValueGetter(in TEffect asset);

    public abstract string Source { get; }
    public abstract string EntryPoint { get; }

    private readonly Dictionary<string, ValueGetter> _propGetters = [];

    protected void RegisterProperty<TPropertyCommand, TValue>(ValueGetter valueGetter)
        where TPropertyCommand : IPropertyCommand<TValue>, ICommand
    {
        _propGetters.Add(TPropertyCommand.PropertyName, valueGetter);

        Listen((EntityRef entity, in TPropertyCommand cmd) => {
            ref var effect = ref entity.Get<TEffect>();
            entity.Modify(new EffectMetadata.SetProperty(TPropertyCommand.PropertyName, valueGetter(effect)));
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
        entity.Modify(ref meta, new EffectMetadata.SetProperties(propsBuilder.ToImmutable()));
    }
}