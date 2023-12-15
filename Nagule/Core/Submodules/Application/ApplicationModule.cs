namespace Nagule;

using Sia;

public class ApplicationQuitSystem : SystemBase
{
    public ApplicationQuitSystem()
    {
        Matcher = Matchers.Of<Application>();
        Trigger = EventUnion.Of<Application.Quit>();
        Filter = EventUnion.Of<HOEvents.Cancel<Application.Quit>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        world.Dispose();
        Environment.Exit(0);
    }
}

public class ApplicationModule : SystemBase
{
    public ApplicationModule()
    {
        Children = SystemChain.Empty
            .Add<ApplicationQuitSystem>();
    }
}