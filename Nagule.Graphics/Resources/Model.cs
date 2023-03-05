namespace Nagule.Graphics;

using System.Collections.Immutable;

public struct ModelProps : IHashComponent
{
    public ReactiveHashSet<Animation> Animations { get; } = new();
    public ReactiveObject<GraphNode> RootNode { get; } = new();

    public ModelProps() {}

    public void Set(Model resource)
    {
        Animations.Clear();
        Animations.UnionWith(resource.Animations);

        RootNode.Value = resource.RootNode;
    }
}

public record Model : ResourceBase<ModelProps>
{
    public ImmutableHashSet<Animation> Animations { get; init; }
        = ImmutableHashSet<Animation>.Empty;
    public GraphNode RootNode { get; init; }

    public Model(GraphNode rootNode)
    {
        RootNode = rootNode;
    }

    public Model WithAnimation(Animation animation)
        => this with { Animations = Animations.Add(animation) };
    public Model WithAnimations(params Animation[] animations)
        => this with { Animations = Animations.Union(animations) };
    public Model WithAnimations(IEnumerable<Animation> animations)
        => this with { Animations = Animations.Union(animations) };
}