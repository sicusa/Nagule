namespace Nagule;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public record struct NodeFeatures
{
    public readonly IReadOnlySet<EntityRef> Set => _features ?? s_emptyFeatures;
    public readonly int Count => _features != null ? _features.Count : 0;

    private HashSet<EntityRef>? _features;
    private List<(EntityRef Entity, RFeatureBase Record)>? _assetFeatures;

    private static readonly HashSet<EntityRef> s_emptyFeatures = [];

    public readonly HashSet<EntityRef>.Enumerator GetEnumerator()
        => (_features ?? s_emptyFeatures).GetEnumerator();

    public readonly EntityRef? Find<TComponent>()
    {
        if (_features != null) {
            foreach (var featureEntity in _features) {
                if (featureEntity.Contains<TComponent>()) {
                    return featureEntity;
                }
            }
        }
        return null;
    }

    public readonly EntityRef Get<TComponent>()
    {
        if (Find<TComponent>() is not EntityRef featureEntity) {
            throw new ComponentNotFoundException(
                "Feature entity with specified component not found: " + typeof(TComponent));
        }
        return featureEntity;
    }
    
    public readonly ref TComponent GetComponentOrNullRef<TComponent>()
    {
        if (_features != null) {
            foreach (var featureEntity in _features) {
                ref var component = ref featureEntity.GetOrNullRef<TComponent>();
                if (!Unsafe.IsNullRef(ref component)) {
                    return ref component!;
                }
            }
        }
        return ref Unsafe.NullRef<TComponent>();
    }
    
    public readonly void Send<TEvent>(World world, in TEvent e)
        where TEvent : IEvent
    {
        if (_features == null) {
            return;
        }
        foreach (var featureEntity in _features) {
            ref var feature = ref featureEntity.Get<Feature>();
            if (feature.IsSelfEnabled) {
                world.Send(featureEntity, e);
            }
        }
    }

    internal void Add(in EntityRef featureEntity)
    {
        _features ??= [];
        _features.Add(featureEntity);
    }

    internal void Add(in EntityRef featureEntity, RFeatureBase record)
    {
        _features ??= [];
        _features.Add(featureEntity);

        _assetFeatures ??= [];
        _assetFeatures.Add((featureEntity, record));
    }

    internal readonly void Remove(RFeatureBase record)
    {
        int index = _assetFeatures!.FindIndex(t => t.Record == record);
        if (index >= _assetFeatures.Count) {
            return;
        }

        var featureEntity = _assetFeatures[index].Entity;
        featureEntity.Dispose();
        _features!.Remove(featureEntity);

        if (!TryShrinkAssetFeaturesList(index)) {
            _assetFeatures[index] = default;
        }
    }

    internal readonly void SetByIndex(int index, EntityRef entity, RFeatureBase record)
    {
        var prevEntity = _assetFeatures![index].Entity;
        if (prevEntity.Valid) {
            _features!.Remove(prevEntity);
            prevEntity.Dispose();
        }

        _features!.Add(entity);
        _assetFeatures[index] = (entity, record);
    }

    internal readonly void RemoveByIndex(int index)
    {
        var prevEntity = _assetFeatures![index].Entity;
        if (prevEntity.Valid) {
            _features!.Remove(prevEntity);
            prevEntity.Dispose();
        }

        if (!TryShrinkAssetFeaturesList(index)) {
            _assetFeatures[index] = default;
        }
    }

    internal void Reset(Span<(EntityRef, RFeatureBase)> features)
    {
        if (features.Length == 0) {
            Clear();
            return;
        }

        _assetFeatures ??= [];
        CollectionsMarshal.SetCount(_assetFeatures, features.Length);
        _features ??= [];

        int index = 0;
        foreach (ref var tuple in features) {
            _features.Add(tuple.Item1);
            _assetFeatures[index] = tuple;
            index++;
        }
    }

    private readonly bool TryShrinkAssetFeaturesList(int index)
    {
        int count = _assetFeatures!.Count;

        if (index != count - 1) {
            return false;
        }

        while (!_assetFeatures[--index].Entity.Valid);
        index++;

        _assetFeatures.RemoveRange(index, count - index);
        return true;
    }

    internal readonly void Clear()
    {
        if (_features == null) {
            return;
        }
        foreach (var feature in _features) {
            feature.Dispose();
        }
        _features.Clear();
        _assetFeatures?.Clear();
    }
}