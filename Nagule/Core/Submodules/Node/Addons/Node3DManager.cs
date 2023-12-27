namespace Nagule;

using Microsoft.Extensions.Logging;
using Sia;

public class Node3DManager : AssetManagerBase<Node3D, Node3DAsset, Node3DState>
{
    private readonly Stack<List<EntityRef>> _entityListPool = new();

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in Transform3D.OnChanged cmd) => {
            var features = entity.GetState<Node3DState>().Features;
            var proxyCmd = new Feature.OnTransformChanged(entity);
            foreach (var featureEntity in features) {
                world.Send(featureEntity, proxyCmd);
            }
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Node3D asset, EntityRef stateEntity)
    {
        var features = _entityListPool.TryPop(out var pooled) ? pooled : [];
        ref var state = ref stateEntity.Get<Node3DState>();
        state.Features = features;

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

    protected override void UnloadAsset(EntityRef entity, ref Node3D asset, EntityRef stateEntity)
    {
        ref var node = ref entity.Get<Node<Transform3D>>();
        foreach (var child in node.Children) {
            child.Destroy();
        }
        ref var state = ref stateEntity.Get<Node3DState>();
        state.Features.Clear();
        state = default;
        _entityListPool.Push(state.Features);
    }

    private static EntityRef CreateFeatureEntity(World world, FeatureAssetBase template, EntityRef nodeEntity)
    {
        var entity = AssetModule.UnsafeCreateEntity(world, template, Tuple.Create(new Feature(nodeEntity)));
        nodeEntity.ReferAsset(entity);
        return entity;
    }
}