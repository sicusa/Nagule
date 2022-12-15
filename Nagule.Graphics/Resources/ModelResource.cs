namespace Nagule.Graphics;

using System.Collections.Immutable;

public record ModelResource : ResourceBase
{
    public ImmutableList<AnimationResource> Animations { get; init; }
        = ImmutableList<AnimationResource>.Empty;
    public GraphNodeResource RootNode { get; init; }

    public ModelResource(GraphNodeResource rootNode)
    {
        RootNode = rootNode;
    }

    public ModelResource WithAnimation(AnimationResource animation)
        => this with { Animations = Animations.Add(animation) };
    public ModelResource WithAnimations(params AnimationResource[] animations)
        => this with { Animations = Animations.AddRange(animations) };
    public ModelResource WithAnimations(IEnumerable<AnimationResource> animations)
        => this with { Animations = Animations.AddRange(animations) };
}