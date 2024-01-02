namespace Nagule.Graphics;

using Sia;

[AfterSystem<CoreModule>]
public class GraphicsModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<RenderPipelineModule>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<RenderFrame>(world);
        AddAddon<MainCamera3D>(world);
    }
}