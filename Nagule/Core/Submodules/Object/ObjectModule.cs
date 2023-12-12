namespace Nagule;

using Sia;

public class EntityDestroyImmediatelySystem : SystemBase
{
    public EntityDestroyImmediatelySystem()
    {
        Matcher = Matchers.Any;
        Trigger = EventUnion.Of<ObjectEvents.DestroyImmediately>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => query.ForEach(entity => entity.Dispose());
}

public class EntityDestroySystem : SystemBase
{
    public EntityDestroySystem()
    {
        Matcher = Matchers.Any;
        Trigger = EventUnion.Of<ObjectEvents.Destroy>();
        Filter = EventUnion.Of<HOEvents.Cancel<ObjectEvents.Destroy>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => query.ForEach(world, static (world, entity) => world.Send(entity, ObjectEvents.DestroyImmediately.Instance));
}

public class ObjectModule : SystemBase
{
    public ObjectModule()
    {
        Children = SystemChain.Empty
            .Add<EntityDestroyImmediatelySystem>()
            .Add<EntityDestroySystem>();
    }
}