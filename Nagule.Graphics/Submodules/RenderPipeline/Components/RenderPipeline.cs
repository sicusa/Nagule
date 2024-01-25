namespace Nagule.Graphics;

using Sia;

public record struct RenderPipeline<TTarget>(
    EntityRef TargetEntity, World World, RenderPipelineScheduler Scheduler);
