namespace Nagule.Graphics;

using Sia;

public class Camera3DPipelineUninitializeSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<ObjectEvents.Destroy>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var lib = world.GetAddon<RenderPipelineLibrary>();

        query.ForEach(lib, static (lib, entity) => {
            if (!lib.EntriesRaw.Remove(entity, out var entry)) {
                return;
            }
            entry.Scheduler.PipelineWorld.Dispose();
            entry.Handle.Dispose();
        });
    }
}