namespace Nagule;

using System.Runtime.CompilerServices;
using Sia;

public static class WorldNameExtensions
{
    public static string GetDisplayName(this EntityRef entity)
    {
        ref var nameId = ref entity.GetOrNullRef<Sid<Name>>();
        if (Unsafe.IsNullRef(ref nameId)) {
            return "(no name)";
        }
        var name = nameId.Value.Value;
        return name == "" ? "(no name)" : name;
    }

    public static EntityRef? FindByName(this World world, string name)
    {
        var aggr = world.GetAddon<Aggregator<Name>>().Find(name);
        return aggr.HasValue ? aggr.Value.First : null;
    }

    public static IReadOnlySet<EntityRef>? FindAllByName(this World world, string name)
    {
        var aggr = world.GetAddon<Aggregator<Name>>().Find(name);
        return aggr.HasValue ? aggr.Value.Group : null;
    }
}