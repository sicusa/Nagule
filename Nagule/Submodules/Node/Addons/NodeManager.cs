namespace Nagule;

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sia;

public class NodeManager<TNode, TNodeRecord> : AssetManager<TNode, TNodeRecord, NodeState>
    where TNode : struct, INode<TNodeRecord>, IAsset<TNodeRecord>
    where TNodeRecord : RNodeBase<TNodeRecord>
{
    private class EventListener(World world) : IEventListener
    {
        public bool OnEvent<TEvent>(in EntityRef entity, in TEvent e) where TEvent : IEvent
        {
            if (typeof(TEvent) == typeof(Transform3D.OnChanged)) {
                var features = entity.GetState<NodeState>().FeaturesRaw;
                if (features == null) { return false; }

                var proxyCmd = new Feature.OnTransformChanged(entity);
                foreach (var featureEntity in features) {
                    world.Send(featureEntity, proxyCmd);
                }
            }
            return false;
        }
    }

    private EventListener? _eventListener;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _eventListener = new(world);
    }

    protected void SetFeatures(in EntityRef entity, ImmutableList<RFeatureAssetBase> records)
    {
        ref var state = ref entity.GetState<NodeState>();
        var features = state.FeaturesRaw;

        if (features != null) {
            foreach (var feature in features) {
                feature.Destroy();
            }
            features.Clear();
        }
        state.AssetFeatures?.Clear();

        RawSetFeatures(entity, ref state, records);
    }

    protected void AddFeature(in EntityRef entity, RFeatureAssetBase record)
    {
        if (CreateFeatureEntity(record, entity) is not EntityRef featureEntity) {
            return;
        }

        ref var state = ref entity.GetState<NodeState>();
        ref var features = ref state.FeaturesRaw;
        ref var assetFeatures = ref state.AssetFeatures;

        features ??= [];
        features.Add(featureEntity);

        assetFeatures ??= [];
        assetFeatures.Add((featureEntity, record));
    }

    protected void SetFeature(in EntityRef entity, int index, RFeatureAssetBase record)
    {
        ref var state = ref entity.GetState<NodeState>();
        var features = state.FeaturesRaw;
        var assetFeatures = state.AssetFeatures;

        if (features == null || assetFeatures == null) {
            return;
        }

        var prevEntity = assetFeatures[index].Entity;
        if (prevEntity.Valid) {
            features.Remove(prevEntity);
            prevEntity.Destroy();
        }

        if (CreateFeatureEntity(record, entity) is EntityRef newEntity) {
            features.Add(newEntity);
            assetFeatures[index] = (newEntity, record);
        }
        else if (!TryShrinkAssetFeaturesList(ref state, index)) {
            assetFeatures[index] = default;
        }
    }

    protected void RemoveFeature(in EntityRef entity, RFeatureAssetBase record)
    {
        ref var state = ref entity.GetState<NodeState>();
        ref var features = ref state.FeaturesRaw;
        ref var assetFeatures = ref state.AssetFeatures;

        if (features == null || assetFeatures == null) {
            return;
        }

        int index = assetFeatures.FindIndex(t => t.Record == record);
        if (index >= assetFeatures.Count) {
            return;
        }

        var featureEntity = assetFeatures[index].Entity;
        featureEntity.Destroy();
        features.Remove(featureEntity);

        if (!TryShrinkAssetFeaturesList(ref state, index)) {
            assetFeatures[index] = default;
        }
    }

    private static bool TryShrinkAssetFeaturesList(ref NodeState state, int index)
    {
        var assetFeatures = state.AssetFeatures!;
        int count = assetFeatures.Count;

        if (index != count - 1) {
            return false;
        }

        while (!assetFeatures[--index].Entity.Valid);
        index++;

        assetFeatures.RemoveRange(index, count - index);
        return true;
    }

    private void RawSetFeatures(
        in EntityRef entity, ref NodeState state, ImmutableList<RFeatureAssetBase> records)
    {
        if (records.Count == 0) {
            state.FeaturesRaw = null;
            state.AssetFeatures = null;
            return;
        }

        ref var features = ref state.FeaturesRaw;
        ref var assetFeatures = ref state.AssetFeatures;

        assetFeatures ??= [];
        CollectionsMarshal.SetCount(assetFeatures, records.Count);

        int index = 0;
        foreach (var record in records) {
            if (CreateFeatureEntity(record, entity) is EntityRef featureEntity) {
                features ??= [];
                features.Add(featureEntity);
                assetFeatures[index] = (featureEntity, record);
            }
        }
    }

    protected override void LoadAsset(EntityRef entity, ref TNode asset, EntityRef stateEntity)
    {
        World.Dispatcher.Listen(entity, _eventListener!);
        foreach (var childNode in asset.Children) {
            TNode.CreateEntity(World, childNode, entity, AssetLife.Persistent);
        }
        RawSetFeatures(entity, ref stateEntity.Get<NodeState>(), asset.Features);
    }

    protected override void UnloadAsset(EntityRef entity, ref TNode asset, EntityRef stateEntity)
    {
        ref var state = ref entity.GetState<NodeState>();
        var features = state.FeaturesRaw;
        if (features != null) {
            foreach (var feature in features) {
                feature.Destroy();
            }
        }
    }

    private EntityRef? CreateFeatureEntity(RFeatureAssetBase record, EntityRef nodeEntity)
    {
        try {
            var entity = AssetSystemModule.UnsafeCreateEntity(
                World, record, Tuple.Create(new Feature(nodeEntity)), AssetLife.Persistent);
            return entity;
        }
        catch (ArgumentException) {
            Logger.LogError("Unrecognized feature '{Feature}' in node '{Node}', skip.",
                record.GetType(), nodeEntity.GetDisplayName());
        }
        catch (Exception e) {
            Logger.LogError("Failed to create entity for feature '{Feature}' in node '{Node}': {Message}",
                record.GetType(), nodeEntity.GetDisplayName(), e.Message);
        }
        return null;
    }
}

public class NodeManager : NodeManager<Node, RNode>;