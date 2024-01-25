namespace Nagule.Graphics;

using Sia;

public class RenderPipelineModule : AddonSystemBase
{
    private SystemHandle? _renderSystemHandle;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Camera3DRenderPipelineCreator>(world);

        var renderFramer = world.GetAddon<RenderFramer>();
        _renderSystemHandle = world.RegisterSystem<Camera3dRenderPipelineTickSystem>(renderFramer.Scheduler);
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        _renderSystemHandle?.Dispose();
        _renderSystemHandle = null;
    }
}