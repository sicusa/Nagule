namespace Nagule;

using Sia;

public class NameModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Aggregator<Name>>(world);
    }
}