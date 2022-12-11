namespace Nagule.Graphics;

public record ModelResource : ResourceBase
{
    public AnimationResource[]? Animations;
    public GraphNodeResource RootNode;

    public ModelResource(GraphNodeResource rootNode)
    {
        RootNode = rootNode;
    }
}