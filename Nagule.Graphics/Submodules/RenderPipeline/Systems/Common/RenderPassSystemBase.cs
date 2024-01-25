namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderPassSystemBase() : AddonSystemBase(matcher: Matchers.Any)
{
    protected EntityRef CameraState { get; private set; }

    [AllowNull] protected World World { get; private set; }
    [AllowNull] protected World MainWorld { get; private set; }
    [AllowNull] protected RenderFramer RenderFramer { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        World = world;

        var info = world.GetAddon<PipelineInfo>();
        CameraState = info.CameraState;
        MainWorld = info.MainWorld;
        RenderFramer = MainWorld.GetAddon<RenderFramer>();
    }
}