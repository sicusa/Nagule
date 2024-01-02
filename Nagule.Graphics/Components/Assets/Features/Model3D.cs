namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(Model3D))]
[NaAsset<Model3D>]
public record RModel3D : FeatureAssetBase
{
    public ImmutableHashSet<Animation> Animations { get; init; } = [];
    public RNode3D RootNode { get; init; }

    public static RModel3D Load(string path)
        => ModelUtils.Load(File.OpenRead(path), path);

    public static RModel3D Load(Stream stream, string? name = null)
        => ModelUtils.Load(stream, name);

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