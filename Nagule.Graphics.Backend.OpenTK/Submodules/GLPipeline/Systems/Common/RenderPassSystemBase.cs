namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderPassSystemBase : RenderSystemBase
{
    [AllowNull] protected EntityRef Camera { get; private set; }
    [AllowNull] protected World Pipeline { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        if (scheduler is not GLPipelineScheduler pipelineScheduler) {
            throw new InvalidOperationException(
                "Render pass systems can only be registered to pipeline scheduler.");
        }
        Camera = pipelineScheduler.PipelineCamera;
        Pipeline = pipelineScheduler.PipelineWorld;
    }
}