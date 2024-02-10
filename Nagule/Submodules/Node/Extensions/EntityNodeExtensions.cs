namespace Nagule;

using Sia;

public static class EntityNodeExtensions
{
    public static EntityRef GetFeature<TComponent>(this EntityRef entity)
        => entity.Get<NodeFeatures>().Get<TComponent>();

    public static EntityRef? FindFeature<TComponent>(this EntityRef entity)
        => entity.Get<NodeFeatures>().Find<TComponent>();
}