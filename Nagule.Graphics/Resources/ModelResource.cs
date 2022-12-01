namespace Nagule.Graphics;

using System.Numerics;

public record ModelResource : IResource
{
    public AnimationResource[]? Animations;
    public GraphNodeResource? RootNode;
}