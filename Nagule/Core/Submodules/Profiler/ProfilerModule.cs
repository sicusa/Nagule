namespace Nagule;

using Sia;

public class ProfilerUpdateSystem : SystemBase
{
    private Profiler? _profiler;
    private SimulationFrame? _frame;

    public ProfilerUpdateSystem()
    {
        Matcher = Matchers.From<TypeUnion<Profiler>>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        _profiler = world.GetAddon<Profiler>();
        _frame = world.GetAddon<SimulationFrame>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        _profiler!.Frame++;
        _profiler.Time = _frame!.Time;
    }
}

public class ProfilerModule : AddonSystemBase
{
    public ProfilerModule()
    {
        Children = SystemChain.Empty
            .Add<ProfilerUpdateSystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Profiler>(world);
    }
}