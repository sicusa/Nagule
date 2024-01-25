namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderPassSystemBase() : AddonSystemBase(matcher: Matchers.Any)
{
    protected EntityRef Camera { get; private set; }

    [AllowNull] protected World World { get; private set; }
    [AllowNull] protected World MainWorld { get; private set; }
    [AllowNull] protected RenderFramer RenderFramer { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        if (scheduler is not RenderPipelineScheduler pipelineScheduler) {
            throw new InvalidOperationException(
                "Render pass systems can only be registered to pipeline scheduler");
        }

        World = world;
        Camera = pipelineScheduler.PipelineCamera;
        MainWorld = world.GetAddon<PipelineInfo>().MainWorld;
        RenderFramer = MainWorld.GetAddon<RenderFramer>();
    }
}