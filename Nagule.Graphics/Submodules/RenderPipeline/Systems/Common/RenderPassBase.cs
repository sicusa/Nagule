namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderPassBase() : AddonSystemBase(matcher: Matchers.Any)
{
    protected EntityRef CameraState { get; private set; }

    [AllowNull] protected World World { get; private set; }
    [AllowNull] protected World MainWorld { get; private set; }
    [AllowNull] protected RenderFramer RenderFramer { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        World = world;

        var info = world.GetAddon<RenderPipelineInfo>();
        CameraState = info.CameraState;
        MainWorld = info.MainWorld;
        RenderFramer = MainWorld.GetAddon<RenderFramer>();
    }
}