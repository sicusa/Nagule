namespace Nagule;

using Sia;

public static class EntityFeatureExtensions
{
    public static ref TComponent GetFeatureNode<TComponent>(this EntityRef entity)
        where TComponent : struct
        => ref entity.Get<Feature>().Node.Get<TComponent>();

    public static EntityRef GetFeatureNode(this EntityRef entity)
        => entity.Get<Feature>().Node;
}