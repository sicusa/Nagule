namespace Nagule.Graphics;

using Sia;

public class RenderPipelineScheduler(EntityRef camera) : Scheduler
{
    public EntityRef PipelineCamera { get; } = camera;
    public World PipelineWorld { get; } = new();
}