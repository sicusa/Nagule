namespace Nagule;

using Sia;

public class GuidModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Mapper<Guid>>(world);
    }
}