namespace Nagule;

using Sia;

public static class EntityNodeExtensions
{
    public static IReadOnlyList<EntityRef> GetFeatures(this EntityRef nodeEntity)
        => nodeEntity.GetState<Node3DState>().FeaturesRaw;
}