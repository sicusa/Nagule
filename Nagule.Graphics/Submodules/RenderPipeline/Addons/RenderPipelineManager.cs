namespace Nagule.Graphics;

using System.Threading;
using Microsoft.Extensions.Logging;
using Sia;

public partial class RenderPipelineManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in RenderPipeline.SetCamera cmd) => {
            if (FindCameraEntity(cmd.Value) is not EntityRef cameraEntity) {
                entity.Dispose();
                return;
            }
            
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderPipelineState>();
                state.CameraEntity = cameraEntity;
                state.World.GetAddon<RenderPipelineInfo>().CameraState = cameraEntity;
            });
        });

        Listen((EntityRef entity, in RenderPipeline.SetPasses cmd) => {
            ref var pipeline = ref entity.Get<RenderPipeline>();
            if (FindCameraEntity(pipeline.Camera) is not EntityRef cameraEntity) {
                entity.Dispose();
                return;
            }

            var passes = cmd.Value;
            var (world, scheduler) = CreatePipeline(cameraEntity, passes);
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderPipelineState>();

                var prevWorld = state.World;
                prevWorld.ClearAddons();
                SimulationFramer.Enqueue(entity, () => prevWorld.Dispose());

                state.CameraEntity = cameraEntity;
                state.World = world;
                state.Scheduler = scheduler;
            });
        });
    }

    public override void LoadAsset(
        in EntityRef entity, ref RenderPipeline asset, EntityRef stateEntity)
    {
        if (FindCameraEntity(asset.Camera) is not EntityRef cameraEntity) {
            entity.Dispose();
            return;
        }

        var (world, scheduler) = CreatePipeline(cameraEntity, asset.Passes);

        RenderFramer.Enqueue(entity, () => {
            stateEntity.Get<RenderPipelineState>() = new RenderPipelineState {
                CameraEntity = cameraEntity,
                World = world,
                Scheduler = scheduler
            };
        });
    }

    private EntityRef? FindCameraEntity(AssetRefer<RCamera3D> refer)
    {
        var cameraEntity = refer.Find<Camera3D>(World);
        if (!cameraEntity.HasValue) {
            Logger.LogError("Failed to load render pipeline: camera not found");
            return null;
        }
        return cameraEntity;
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

    public override CancellationToken? DestroyState(in EntityRef entity, in RenderPipeline asset, EntityRef stateEntity)
    {
        var source = new CancellationTokenSource();
        RenderFramer.Enqueue(entity, () => {
            var pipelineWorld = stateEntity.Get<RenderPipelineState>().World;
            pipelineWorld.ClearAddons();

            SimulationFramer.Start(() => {
                pipelineWorld.Dispose();
                source.Cancel();
            });
        });
        return source.Token;
    }
}