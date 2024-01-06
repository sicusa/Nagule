namespace Nagule;

using Sia;

public static class EntityNodeExtensions
{
    public static IReadOnlySet<EntityRef> GetFeatures(this EntityRef nodeEntity)
        => nodeEntity.GetState<NodeState>().Features;
}