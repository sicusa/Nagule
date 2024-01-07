namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

public record struct Model3DLoadOptions(bool IsOccluder = false)
{
    public static readonly Model3DLoadOptions Default = new();
}

[SiaTemplate(nameof(Model3D))]
[NaAsset]
public record RModel3D : RFeatureBase, ILoadableAsset<RModel3D, Model3DLoadOptions>
{
    public static RModel3D Load(Stream stream, string? name = null)
        => Load(stream, Model3DLoadOptions.Default, name);

    public static RModel3D Load(Stream stream, Model3DLoadOptions options, string? name = null)
        => ModelUtils.Load(stream, name, options.IsOccluder);

    public ImmutableHashSet<Animation> Animations { get; init; } = [];
    public RNode3D RootNode { get; init; }

    public RModel3D(RNode3D rootNode)
    {
        RootNode = rootNode;
    }

    public RModel3D WithAnimation(Animation animation)
        => this with { Animations = Animations.Add(animation) };
    public RModel3D WithAnimations(params Animation[] animations)
        => this with { Animations = Animations.Union(animations) };
    public RModel3D WithAnimations(IEnumerable<Animation> animations)
        => this with { Animations = Animations.Union(animations) };
}