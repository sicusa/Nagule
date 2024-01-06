namespace Nagule;

using Sia;

public class PeripheralModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Peripheral>(world);
        AddAddon<PrimaryWindow>(world);
    }
}