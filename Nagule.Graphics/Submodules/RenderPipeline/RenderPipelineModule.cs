namespace Nagule.Graphics;

using Sia;

public class RenderPipelineModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<Camera3DPipelineInitializeSystem>()
            .Add<Camera3DPipelineUninitializeSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<RenderPipelineLibrary>(world);
    }
}