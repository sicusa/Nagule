namespace Nagule;

using Sia;

[AfterSystem<AssetSystemModule>]
public class NodeModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Node3DManager>(world);
    }
}