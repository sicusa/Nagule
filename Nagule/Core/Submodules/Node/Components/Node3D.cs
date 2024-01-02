namespace Nagule;

using System.Collections.Immutable;
using System.Numerics;
using Sia;

[SiaTemplate(nameof(Node3D))]
public record RNode3D : AssetBase
{
    public static RNode3D Empty { get; } = new();

    [SiaIgnore] public Vector3 Position { get; init; } = Vector3.Zero;
    [SiaIgnore] public Vector3 Rotation { get; init; } = Vector3.Zero;
    [SiaIgnore] public Vector3 Scale { get; init; } = Vector3.One;

    [SiaProperty(NoCommands = true)]
    public ImmutableList<RNode3D> Children { get; init; } = [];

    [SiaProperty(NoCommands = true)]
    public ImmutableList<FeatureAssetBase> Features { get; init; } = [];

    [SiaProperty(Item = "MetadataEntry")]
    public ImmutableDictionary<string, Dyn> Metadata { get; init; } = ImmutableDictionary<string, Dyn>.Empty;

    public RNode3D Recurse(
        Func<Func<RNode3D, RNode3D>, RNode3D, RNode3D> mapper)
    {
        RNode3D DoRecurse(RNode3D node) => mapper(DoRecurse, node);
        return mapper(DoRecurse, this);
    }

    public RNode3D Recurse<TArg>(
        Func<Func<RNode3D, TArg, RNode3D>, RNode3D, TArg, RNode3D> mapper, TArg initial)
    {
        RNode3D DoRecurse(RNode3D node, TArg arg) => mapper(DoRecurse, node, arg);
        return mapper(DoRecurse, this, initial);
    }

    public RNode3D WithChild(RNode3D child)
        => this with { Children = Children.Add(child) };
    public RNode3D WithChildren(params RNode3D[] children)
        => this with { Children = Children.AddRange(children) };
    public RNode3D WithChildren(IEnumerable<RNode3D> children)
        => this with { Children = Children.AddRange(children) };

    public RNode3D WithFeature(FeatureAssetBase child)
        => this with { Features = Features.Add(child) };
    public RNode3D WithFeatures(params FeatureAssetBase[] children)
        => this with { Features = Features.AddRange(children) };
    public RNode3D WithFeatures(IEnumerable<FeatureAssetBase> children)
        => this with { Features = Features.AddRange(children) };

    public RNode3D WithMetadata(string key, Dyn value)
        => this with { Metadata = Metadata.SetItem(key, value) };
    public RNode3D WithMetadata(params KeyValuePair<string, Dyn>[] entries)
        => this with { Metadata = Metadata.SetItems(entries) };
    public RNode3D WithMetadata(IEnumerable<KeyValuePair<string, Dyn>> entries)
        => this with { Metadata = Metadata.SetItems(entries) };
}

public partial record struct Node3D : IAsset<RNode3D>
{
    public static EntityRef CreateEntity(
        World world, RNode3D template, AssetLife life = AssetLife.Persistent)
        => world.CreateInBucketHost(Tuple.Create(
            AssetBundle.Create(new Node3D(template), life),
            Sid.From<IAsset>(template),
            new Transform3D(template.Position, template.Rotation.ToQuaternion(), template.Scale),
            new Node<Transform3D>()
        ));

    public static EntityRef CreateEntity<TComponentBundle>(
        World world, RNode3D template, in TComponentBundle bundle, AssetLife life = AssetLife.Persistent)
        where TComponentBundle : struct, IComponentBundle
        => world.CreateInBucketHost(Tuple.Create(
            AssetBundle.Create(new Node3D(template), life),
            Sid.From<IAsset>(template),
            new Transform3D(template.Position, template.Rotation.ToQuaternion(), template.Scale),
            new Node<Transform3D>(),
            bundle
        ));

    public static EntityRef CreateEntity(
        World world, RNode3D template, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = CreateEntity(world, template, life);
        referrer.Modify(new AssetMetadata.Refer(entity));
        return entity;
    }
}