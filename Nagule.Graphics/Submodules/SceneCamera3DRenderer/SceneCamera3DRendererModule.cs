namespace Nagule.Graphics;

using Sia;

[AfterSystem<RenderPipelineModule>]
public class SceneCamera3DRendererModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<SceneCamera3DManager>(world);
        AddAddon<SceneCamera3DRenderer>(world);
    }
}