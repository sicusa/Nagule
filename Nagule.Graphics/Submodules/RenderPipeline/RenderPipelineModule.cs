namespace Nagule.Graphics;

using Sia;

public class RenderPipelineModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<RenderPipelineLibrary>(world);
        AddAddon<Camera3DPipelineManager>(world);
    }
}