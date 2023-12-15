namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class Camera3DPipelineManager : ViewBase<TypeUnion<Camera3D>>
{
    private record struct PipelineEntry(GLPipelineScheduler Scheduler, SystemChain.Handle Handle);

    [AllowNull] internal SystemChain PipelineChain { get; set; }

    private readonly Dictionary<EntityRef, PipelineEntry> _pipelines = [];

    protected override void OnEntityAdded(in EntityRef entity)
    {
        var pipelineScheduler = new GLPipelineScheduler(entity);
        _pipelines[entity] = new(pipelineScheduler, PipelineChain.RegisterTo(World, pipelineScheduler));
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        if (!_pipelines.Remove(entity, out var entry)) {
            return;
        }
        entry.Scheduler.PipelineWorld.Dispose();
        entry.Handle.Dispose();
    }
}