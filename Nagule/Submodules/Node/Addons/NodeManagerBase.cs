namespace Nagule;

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.Extensions.Logging;
using Sia;

public abstract class NodeManagerBase<TNode, TNodeRecord> : AssetManagerBase<TNode>
    where TNode : struct, INode<TNodeRecord>
    where TNodeRecord : RNodeBase<TNodeRecord>
{
    public override void LoadAsset(in EntityRef entity, ref TNode asset, EntityRef stateEntity)
    {
        ref var hierarchy = ref entity.Get<NodeHierarchy>();
        hierarchy.IsEnabled = asset.IsEnabled;
        RawSetFeatures(entity, ref entity.Get<NodeFeatures>(), asset.Features);
    }

    public override void UnloadAsset(in EntityRef entity, in TNode asset, EntityRef stateEntity)
    {
        ref var features = ref entity.Get<NodeFeatures>();
        features.Clear();

        ref var hierarchy = ref entity.Get<NodeHierarchy>();
        int childrenCount = hierarchy.Children.Count;

        if (childrenCount != 0) {
            using var children = SpanOwner<EntityRef>.Allocate(childrenCount);
            var childrenSpan = children.Span;
            childrenCount = 0;

            foreach (var child in hierarchy) {
                childrenSpan[childrenCount] = child;
                childrenCount++;
            }
            for (int i = 0; i != childrenCount; ++i) {
                childrenSpan[i].Dispose();
            }
        }

        hierarchy.Parent = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected static bool HandleStandardEvents<
            TEvent,
            TTransformChangedEvent,
            TSetIsEnabledCommand
        >(World world, in EntityRef entity, in TEvent e)
        where TEvent : IEvent
        where TTransformChangedEvent : IEvent
        where TSetIsEnabledCommand : ICommand
    {
        var eventType = typeof(TEvent);
        if (eventType == typeof(TTransformChangedEvent)) {
            NotifyTransformChangedEvent(world, entity);
            return true;
        }
        else if (eventType == typeof(TSetIsEnabledCommand)) {
            SetNodeIsEnabledRecursively(world, entity, true);
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void NotifyTransformChangedEvent(World world, in EntityRef nodeEntity)
    {
        if (!nodeEntity.Get<NodeHierarchy>().IsEnabled) {
            return;
        }
        nodeEntity.Get<NodeFeatures>().Send(
            world, new Feature.OnNodeTransformChanged(nodeEntity));
    }

    private static void SetNodeIsEnabledRecursively(World world, in EntityRef nodeEntity, bool parentEnabled)
    {
        ref var node = ref nodeEntity.Get<TNode>();
        ref var hierarchy = ref nodeEntity.Get<NodeHierarchy>();
        var isEnabled = parentEnabled && node.IsEnabled;

        if (hierarchy.IsEnabled == isEnabled) {
            return;
        }
        hierarchy.IsEnabled = isEnabled;

        world.Send(nodeEntity, new NodeHierarchy.OnIsEnabledChanged(isEnabled));
        nodeEntity.Get<NodeFeatures>().Send(world, Feature.OnIsEnabledChanged.Instance);

        foreach (var child in hierarchy) {
            SetNodeIsEnabledRecursively(world, child, isEnabled);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetFeatures(in EntityRef entity, ImmutableList<RFeatureBase> records)
    {
        ref var features = ref entity.Get<NodeFeatures>();
        RawSetFeatures(entity, ref features, records);
    }

    private void RawSetFeatures(
        EntityRef entity, ref NodeFeatures features, ImmutableList<RFeatureBase> records)
    {
        int count = records.Count;
        if (count == 0) {
            features.Clear();
            return;
        }

        using var spanOwner = SpanOwner<(EntityRef, RFeatureBase)>.Allocate(count);
        var span = spanOwner.Span;

        int index = 0;
        foreach (var record in records) {
            if (CreateFeatureEntity(record, entity) is EntityRef featureEntity) {
                span[index] = (featureEntity, record);
                index++;
            }
        }
        
        features.Reset(span[..index]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AddFeature(in EntityRef entity, RFeatureBase record)
    {
        if (CreateFeatureEntity(record, entity) is not EntityRef featureEntity) {
            return;
        }
        entity.Get<NodeFeatures>().Add(featureEntity, record);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetFeature(in EntityRef entity, int index, RFeatureBase record)
    {
        ref var features = ref entity.Get<NodeFeatures>();
        if (features.Count == 0) { return; }

        if (CreateFeatureEntity(record, entity) is EntityRef featureEntity) {
            features.SetByIndex(index, featureEntity, record);
        }
        else {
            features.RemoveByIndex(index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RemoveFeature(in EntityRef entity, RFeatureBase record)
    {
        ref var features = ref entity.Get<NodeFeatures>();
        if (features.Count == 0) { return; }
        features.Remove(record);
    }

    private EntityRef? CreateFeatureEntity(RFeatureBase record, EntityRef nodeEntity)
    {
        try {
            var satisfiedFeatureTypes =
                nodeEntity.Get<TNode>().Features.Select(f => f.GetType());
            return FeatureUtils.RawCreateEntity(
                World, record, nodeEntity, satisfiedFeatureTypes);
        }
        catch (ArgumentException) {
            Logger.LogError("[{Name}] Unrecognized feature '{Feature}', skip.",
                nodeEntity.GetDisplayName(), record.GetType());
        }
        catch (Exception e) {
            Logger.LogError("[{Node}] Failed to create entity for feature '{Feature}': {Message}",
                nodeEntity.GetDisplayName(), record.GetType(), e.Message);
        }
        return null;
    }
}