namespace Nagule.Graphics;

using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Model3D))]
[NaguleAsset<Model3D>]
public record Model3DAsset : FeatureAssetBase
{
    public ImmutableHashSet<Animation> Animations { get; init; }
        = ImmutableHashSet<Animation>.Empty;
    public Node3DAsset RootNode { get; init; }

    public static Model3DAsset Load(string path)
        => ModelUtils.Load(File.OpenRead(path), path);

    public static Model3DAsset Load(Stream stream, string? name = null)
        => ModelUtils.Load(stream, name);

    public Model3DAsset(Node3DAsset rootNode)
    {
        RootNode = rootNode;
    }

    public Model3DAsset WithAnimation(Animation animation)
        => this with { Animations = Animations.Add(animation) };
    public Model3DAsset WithAnimations(params Animation[] animations)
        => this with { Animations = Animations.Union(animations) };
    public Model3DAsset WithAnimations(IEnumerable<Animation> animations)
        => this with { Animations = Animations.Union(animations) };
}