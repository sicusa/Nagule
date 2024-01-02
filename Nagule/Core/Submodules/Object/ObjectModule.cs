namespace Nagule;

using Sia;

public class EntityDestroyImmediatelySystem()
    : SystemBase(
        matcher: Matchers.Any,
        trigger: EventUnion.Of<ObjectEvents.DestroyImmediately>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => query.ForEach(entity => entity.Dispose());
}

public class EntityDestroySystem()
    : SystemBase(
        matcher: Matchers.Any,
        trigger: EventUnion.Of<ObjectEvents.Destroy>(),
        filter: EventUnion.Of<HOEvents.Cancel<ObjectEvents.Destroy>>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => query.ForEach(world, static (world, entity) => world.Send(entity, ObjectEvents.DestroyImmediately.Instance));
}

public class ObjectModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<EntityDestroyImmediatelySystem>()
            .Add<EntityDestroySystem>());