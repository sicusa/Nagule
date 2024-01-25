namespace Nagule.Graphics;

using Sia;

[AfterSystem<RenderPipelineModule>]
public class Camera3DRenderPipelineModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Camera3DRenderer>(world);
        AddAddon<Camera3DRenderPipelineManager>(world);
    }
}