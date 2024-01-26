namespace Nagule.Graphics;

using Microsoft.Extensions.Logging;
using Sia;

public partial class RenderPipelineManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef e, in RenderPipeline.SetPasses cmd) => {
            var stateEntity = e.GetStateEntity();

            ref var state = ref stateEntity.Get<RenderPipelineState>();
            var (world, scheduler) = CreatePipeline(state.CameraEntity, cmd.Value);

            var simFramer = World.GetAddon<SimulationFramer>();
            var entries = World.GetAddon<PipelineRenderer>().Entries;

            RenderFramer.Enqueue(e, () => {
                ref var state = ref stateEntity.Get<RenderPipelineState>();
                var prevWorld = state.World;
                var prevScheduler = state.Scheduler;
                var priority = state.Priority;

                simFramer.Start(() => prevWorld.Dispose());

                var index = entries.FindIndex(e => e.Scheduler == prevScheduler);
                entries[index] = new(priority, scheduler);

                state.World = world;
                state.Scheduler = scheduler;
            });
        });

        Listen((in EntityRef e, in RenderPipeline.SetPriority cmd) => {
            var priority = cmd.Value;
            var entries = World.GetAddon<PipelineRenderer>().Entries;
            var stateEntity = e.GetStateEntity();

            RenderFramer.Enqueue(e, () => {
                ref var state = ref stateEntity.Get<RenderPipelineState>();
                var scheduler = state.Scheduler;

                int prevIndex = entries.FindIndex(e => e.Scheduler == scheduler);
                entries.RemoveAt(prevIndex);

                var newIndex = entries.FindIndex(e => e.Priority >= priority);
                if (newIndex == -1) {
                    newIndex = entries.Count;
                }
                entries.Insert(newIndex, new(priority, scheduler));
                state.Priority = priority;
            });
        });
    }

    protected override void LoadAsset(
        EntityRef entity, ref RenderPipeline asset, EntityRef stateEntity)
    {
        var cameraEntity = asset.Camera.Find(World);
        if (!cameraEntity.HasValue) {
            Logger.LogError("Failed to load render pipeline: camera not found");
            return;
        }

        var (world, scheduler) = CreatePipeline(cameraEntity.Value, asset.Passes);
        var priority = asset.Priority;
        var entries = World.GetAddon<PipelineRenderer>().Entries;

        RenderFramer.Enqueue(entity, () => {
            stateEntity.Get<RenderPipelineState>() = new RenderPipelineState {
                CameraEntity = cameraEntity.Value,
                World = world,
                Scheduler = scheduler,
                Priority = priority
            };
            var index = entries.FindIndex(e => e.Priority >= priority);
            if (index == -1) {
                index = entries.Count;
            }
            entries.Insert(index, new(priority, scheduler));
        });
    }

    private (World, Scheduler) CreatePipeline(in EntityRef cameraEntity, RenderPassChain passes)
    {
        var pipelineWorld = new World();
        var pipelineScheduler = new Scheduler();

        var info = pipelineWorld.AcquireAddon<RenderPipelineInfo>();
        info.MainWorld = World;
        info.CameraState = cameraEntity.GetStateEntity();

        passes.RegisterTo(pipelineWorld, pipelineScheduler);
        return (pipelineWorld, pipelineScheduler);
    }

    protected override void UnloadAsset(EntityRef entity, ref RenderPipeline asset, EntityRef stateEntity)
    {
        var simFramer = World.GetAddon<SimulationFramer>();
        var entries = World.GetAddon<PipelineRenderer>().Entries;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<RenderPipelineState>();
            entries.Remove(new(state.Priority, state.Scheduler));

            var world = state.World;
            simFramer.Start(() => world.Dispose());
        });
    }
}
