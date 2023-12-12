namespace Nagule;

using System.Collections.Immutable;
using System.Numerics;
using Sia;

[SiaTemplate(nameof(Node3D))]
public record Node3DAsset : AssetBase
{
    public static Node3DAsset Empty { get; } = new();

    [SiaIgnore] public Vector3 Position { get; init; } = Vector3.Zero;
    [SiaIgnore] public Quaternion Rotation { get; init; } = Quaternion.Identity;
    [SiaIgnore] public Vector3 Scale { get; init; } = Vector3.One;

    [SiaProperty(NoCommands = true)]
    public ImmutableList<Node3DAsset> Children { get; init; } = ImmutableList<Node3DAsset>.Empty;

    [SiaProperty(NoCommands = true)]
    public ImmutableList<FeatureAssetBase> Features { get; init; } = ImmutableList<FeatureAssetBase>.Empty;

    [SiaProperty(Item = "MetadataEntry")]
    public ImmutableDictionary<string, Dyn> Metadata { get; init; } = ImmutableDictionary<string, Dyn>.Empty;

    public Node3DAsset Recurse(
        Func<Func<Node3DAsset, Node3DAsset>, Node3DAsset, Node3DAsset> mapper)
    {
        Node3DAsset DoRecurse(Node3DAsset node) => mapper(DoRecurse, node);
        return mapper(DoRecurse, this);
    }

    public Node3DAsset Recurse<TArg>(
        Func<Func<Node3DAsset, TArg, Node3DAsset>, Node3DAsset, TArg, Node3DAsset> mapper, TArg initial)
    {
        Node3DAsset DoRecurse(Node3DAsset node, TArg arg) => mapper(DoRecurse, node, arg);
        return mapper(DoRecurse, this, initial);
    }

    public Node3DAsset WithChild(Node3DAsset child)
        => this with { Children = Children.Add(child) };
    public Node3DAsset WithChildren(params Node3DAsset[] children)
        => this with { Children = Children.AddRange(children) };
    public Node3DAsset WithChildren(IEnumerable<Node3DAsset> children)
        => this with { Children = Children.AddRange(children) };

    public Node3DAsset WithFeature(FeatureAssetBase child)
        => this with { Features = Features.Add(child) };
    public Node3DAsset WithFeatures(params FeatureAssetBase[] children)
        => this with { Features = Features.AddRange(children) };
    public Node3DAsset WithFeatures(IEnumerable<FeatureAssetBase> children)
        => this with { Features = Features.AddRange(children) };

    public Node3DAsset WithMetadata(string key, Dyn value)
        => this with { Metadata = Metadata.SetItem(key, value) };
    public Node3DAsset WithMetadata(params KeyValuePair<string, Dyn>[] entries)
        => this with { Metadata = Metadata.SetItems(entries) };
    public Node3DAsset WithMetadata(IEnumerable<KeyValuePair<string, Dyn>> entries)
        => this with { Metadata = Metadata.SetItems(entries) };
}

public partial record struct Node3D : IAsset<Node3DAsset>
{
    public static EntityRef CreateEntity(
        World world, Node3DAsset template, AssetLife life = AssetLife.Persistent)
        => world.CreateInBucketHost(Tuple.Create(
            AssetBundle.Create(new Node3D(template), life),
            Sid.From<IAsset>(template),
            new Transform3D(template.Position, template.Rotation, template.Scale),
            new Node<Transform3D>()
        ));

    public static EntityRef CreateEntity<TComponentBundle>(
        World world, Node3DAsset template, in TComponentBundle bundle, AssetLife life = AssetLife.Persistent)
        where TComponentBundle : struct, IComponentBundle
        => world.CreateInBucketHost(Tuple.Create(
            AssetBundle.Create(new Node3D(template), life),
            Sid.From<IAsset>(template),
            new Transform3D(template.Position, template.Rotation, template.Scale),
            new Node<Transform3D>(),
            bundle
        ));

    public static EntityRef CreateEntity(
        World world, Node3DAsset template, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = CreateEntity(world, template, life);
        referrer.Modify(new AssetMetadata.Refer(entity));
        return entity;
    }
}