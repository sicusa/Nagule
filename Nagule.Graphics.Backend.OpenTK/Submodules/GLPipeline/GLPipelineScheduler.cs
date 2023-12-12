namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class GLPipelineScheduler : Scheduler
{
    public EntityRef PipelineCamera { get; }
    public World PipelineWorld { get; } = new();

    public GLPipelineScheduler(EntityRef camera)
    {
        PipelineCamera = camera;
    }
}