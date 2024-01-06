namespace Nagule;

using Sia;

public class ApplicationQuitSystem()
    : SystemBase(
        matcher: Matchers.Of<Application>(),
        trigger: EventUnion.Of<Application.Quit>(),
        filter: EventUnion.Of<HOEvents.Cancel<Application.Quit>>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        world.Dispose();
        Environment.Exit(0);
    }
}

public class ApplicationModule()
    : SystemBase(
        children: SystemChain.Empty
            .Add<ApplicationQuitSystem>());