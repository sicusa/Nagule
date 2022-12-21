namespace Nagule.Graphics;

using System.Collections.Immutable;

public record Model : ResourceBase
{
    public ImmutableList<Animation> Animations { get; init; }
        = ImmutableList<Animation>.Empty;
    public GraphNode RootNode { get; init; }

    public Model(GraphNode rootNode)
    {
        RootNode = rootNode;
    }

    public Model WithAnimation(Animation animation)
        => this with { Animations = Animations.Add(animation) };
    public Model WithAnimations(params Animation[] animations)
        => this with { Animations = Animations.AddRange(animations) };
    public Model WithAnimations(IEnumerable<Animation> animations)
        => this with { Animations = Animations.AddRange(animations) };
}