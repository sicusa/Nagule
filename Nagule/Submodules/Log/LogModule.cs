namespace Nagule;

using Sia;

public class LogModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<LogLibrary>(world);
    }
}