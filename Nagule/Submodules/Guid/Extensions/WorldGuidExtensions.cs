namespace Nagule;

using Sia;

public static class WorldGuidExtensions
{
    public static EntityRef? FindById(this World world, in Guid id)
    {
        var aggr = world.GetAddon<Aggregator<Guid>>().Find(id);
        return aggr.HasValue ? aggr.Value.First : null;
    }

    public static IReadOnlySet<EntityRef>? FindAllById(this World world, in Guid id)
    {
        var aggr = world.GetAddon<Aggregator<Guid>>().Find(id);
        return aggr.HasValue ? aggr.Value.Group : null;
    }
}