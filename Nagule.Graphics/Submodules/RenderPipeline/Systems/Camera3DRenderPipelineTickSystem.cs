namespace Nagule.Graphics;

using Sia;

public class Camera3dRenderPipelineTickSystem()
    : SystemBase(
        matcher: Matchers.Of<RenderPipeline<Camera3D>>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        if (query.Count == 0) { return; }

        using var mem = query.Record(static (in EntityRef entity, ref (int Depth, Scheduler Scheduler) result) => {
            ref var pipeline = ref entity.Get<RenderPipeline<Camera3D>>();
            result = (pipeline.TargetEntity.Get<Camera3D>().Depth, pipeline.Scheduler);
        });

        mem.Span.Sort((e1, e2) => e1.Depth.CompareTo(e2.Depth));

        foreach (ref var entry in mem.Span) {
            entry.Scheduler.Tick();
        }

        mem.Dispose();
    }
}