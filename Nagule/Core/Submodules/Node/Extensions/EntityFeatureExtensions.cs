namespace Nagule;

using Sia;

public static class EntityFeatureExtensions
{
    public static EntityRef GetFeatureNode(this EntityRef entity)
        => entity.Get<Feature>().Node;
}