namespace Nagule;

using Sia;

public class ProfilerUpdateSystem()
    : SystemBase(
        matcher: Matchers.From<TypeUnion<Profiler>>())
{
    private Profiler? _profiler;
    private SimulationFramer? _framer;

    public override void Initialize(World world, Scheduler scheduler)
    {
        _profiler = world.GetAddon<Profiler>();
        _framer = world.GetAddon<SimulationFramer>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        _profiler!.Frame++;
        _profiler.Time = _framer!.Time;
    }
}

public class ProfilerModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<ProfilerUpdateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Profiler>(world);
    }
}