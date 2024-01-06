namespace Nagule;

using Sia;

public class TransformModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Hierarchy<Transform3D>>(world);
    }
}