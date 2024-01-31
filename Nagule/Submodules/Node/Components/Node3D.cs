namespace Nagule;

using System.Numerics;
using Sia;

[SiaTemplate(nameof(Node3D))]
public record RNode3D : RNodeBase<RNode3D>
{
    public static RNode3D Empty { get; } = new();

    [SiaIgnore] public Vector3 Position { get; init; } = Vector3.Zero;
    [SiaIgnore] public Vector3 Rotation { get; init; } = Vector3.Zero;
    [SiaIgnore] public Vector3 Scale { get; init; } = Vector3.One;
}

public partial record struct Node3D : INode, IAsset<RNode3D>
{
    static Node3D()
    {
        AssetLibrary.RegisterAsset<Node3D, RNode3D>();
    }

    public static EntityRef CreateEntity(
        World world, RNode3D record, AssetLife life = AssetLife.Persistent)
        => NodeUtils.CreateEntity(world, new Node3D(record), record,
            Bundle.Create(
                new Transform3D(record.Position, record.Rotation.ToQuaternion(), record.Scale)),
            life);

    public static EntityRef CreateEntity<TComponentBundle>(
        World world, RNode3D record, in TComponentBundle bundle, AssetLife life = AssetLife.Persistent)
        where TComponentBundle : struct, IComponentBundle
        => NodeUtils.CreateEntity(world, new Node3D(record), record,
            Bundle.Create(
                new Transform3D(record.Position, record.Rotation.ToQuaternion(), record.Scale),
                bundle),
            life);

    public static EntityRef CreateEntity(
        World world, RNode3D record, EntityRef parent, AssetLife life = AssetLife.Persistent)
    {
        if (!parent.Contains<Node3D>()) {
            throw new ArgumentException("Invalid parent node");
        }
        var entity = CreateEntity(world, record, life);
        parent.Refer(entity);
        entity.NodeHierarchy_SetParent(parent);
        return entity;
    }
}