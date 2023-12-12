namespace Nagule;

using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using Sia;

public class Node3DManager : AssetManagerBase<Node3D, Node3DAsset>
{
    internal class NodeData
    {
        public List<EntityRef> Features { get; } = [];

        public void Clear()
        {
            Features.Clear();
        }
    }

    internal readonly Dictionary<EntityRef, NodeData> _dataDict = [];
    private readonly Stack<NodeData> _dataPool = new();

    public IReadOnlyList<EntityRef>? GetFeatures(EntityRef node)
        => _dataDict.TryGetValue(node, out var list) ? list.Features : null;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in Transform3D.OnChanged cmd) => {
            if (_dataDict.TryGetValue(entity, out var data)) {
            var proxyCmd = new Feature.OnTransformChanged(entity);
                foreach (var featureEntity in data.Features.AsSpan()) {
                    world.Send(featureEntity, proxyCmd);
                }
            }
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Node3D asset)
    {
        var data = _dataPool.TryPop(out var pooled) ? pooled : new();
        _dataDict[entity] = data;

        var features = data.Features;
        var parentCmd = new Transform3D.SetParent(entity);

        foreach (var childNode in asset.Children) {
            var child = Node3D.CreateEntity(World, childNode, entity);
            child.Modify(parentCmd);
        }

        foreach (var feature in asset.Features) {
            try {
                features.Add(CreateFeatureEntity(World, feature, entity));
            }
            catch (ArgumentException e) {
                Logger.LogError("Failed to create feature entity: {Message}", e.Message);
            }
        }
    }

    protected override void UnloadAsset(EntityRef entity, ref Node3D asset)
    {
        ref var node = ref entity.Get<Node<Transform3D>>();
        foreach (var child in node.Children) {
            World.Destroy(child);
        }
        if (!_dataDict.Remove(entity, out var data)) {
            return;
        }
        data.Clear();
        _dataPool.Push(data);
    }

    private static EntityRef CreateFeatureEntity(World world, FeatureAssetBase template, EntityRef nodeEntity)
    {
        var entity = AssetModule.UnsafeCreateEntity(world, template, Tuple.Create(new Feature(nodeEntity)));
        nodeEntity.ReferAsset(entity);
        return entity;
    }
}