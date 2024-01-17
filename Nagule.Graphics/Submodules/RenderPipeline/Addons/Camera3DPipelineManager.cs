namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class Camera3DPipelineManager : ViewBase<TypeUnion<Camera3D>>
{
    protected override void OnEntityAdded(in EntityRef inEntity)
    {
        var camera = inEntity;

        World.GetAddon<SimulationFramer>().Start(() => {
            var lib = World.GetAddon<RenderPipelineLibrary>();

            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
                lib.EntriesRaw, camera, out bool exists);
            if (exists) {
                throw new NaguleInternalException("This should not happen!");
            }

            var pipelineScheduler = new RenderPipelineScheduler(camera);
            var pipelineChain = SystemChain.Empty;

            foreach (var featureEntity in camera.GetFeatureNode().GetFeatures()) {
                ref var provider = ref featureEntity.GetStateOrNullRef<RenderPipelineProvider>();
                if (Unsafe.IsNullRef(ref provider)) {
                    continue;
                }
                pipelineChain = provider.Instance?.TransformPipeline(featureEntity, pipelineChain) ?? pipelineChain;
            }

            entry = new(pipelineScheduler, pipelineChain.RegisterTo(World, pipelineScheduler));
            return true;
        });
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        var lib = World.GetAddon<RenderPipelineLibrary>();
        if (!lib.EntriesRaw.Remove(entity, out var entry)) {
            return;
        }
        World.GetAddon<RenderFramer>().Start(() => {
            entry.Scheduler.PipelineWorld.Dispose();
            entry.Handle.Dispose();
            return true;
        });
    }
}