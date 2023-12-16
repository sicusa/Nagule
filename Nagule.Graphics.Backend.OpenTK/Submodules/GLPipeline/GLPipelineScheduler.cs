namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class GLPipelineScheduler(EntityRef camera) : Scheduler
{
    public EntityRef PipelineCamera { get; } = camera;
    public World PipelineWorld { get; } = new();
}