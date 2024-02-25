namespace Nagule;

using Sia;

public static class NodeUtils
{
    public static EntityRef CreateEntity<TNode, TNodeRecord, TComponentBundle>(
        World world, in TNode node, TNodeRecord record, in TComponentBundle bundle, AssetLife life = AssetLife.Persistent)
        where TNode : struct, INode<TNodeRecord>
        where TNodeRecord : RNodeBase<TNodeRecord>
        where TComponentBundle : IComponentBundle
    {
        var entity = world.CreateInBucketHost(Bundle.Create(
            AssetBundle.Create(world, node, life, record),
            new NodeHierarchy(),
            new NodeFeatures(),
            bundle
        ));
        entity.Get<NodeHierarchy>()._self = entity;

        var cmd = new NodeHierarchy.SetParent(entity);
        foreach (var childNode in record.Children) {
            var childEntity = world.CreateAsset(childNode, entity, AssetLife.Persistent);
            world.Modify(childEntity, cmd);
        }
        return entity;
    }
}