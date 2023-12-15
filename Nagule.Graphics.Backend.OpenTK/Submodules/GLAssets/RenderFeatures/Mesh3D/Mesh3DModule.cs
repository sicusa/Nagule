namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class Mesh3DModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Mesh3DManager>(world);
    }
}
