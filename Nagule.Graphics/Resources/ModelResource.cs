namespace Nagule.Graphics;

using System.Numerics;

public record ModelResource : ResourceBase
{
    public AnimationResource[]? Animations;
    public GraphNodeResource? RootNode;
}