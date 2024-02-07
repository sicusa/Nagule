namespace Nagule.Graphics;

using System.Threading;
using Microsoft.Extensions.Logging;
using Sia;

public partial class RenderPipelineManager
{
    public override void LoadAsset(
        in EntityRef entity, ref RenderPipeline asset, EntityRef stateEntity)
    {
        var cameraEntity = asset.Camera.Find(World);
        if (!cameraEntity.HasValue) {
            Logger.LogError("Failed to load render pipeline: camera not found");
            return;
        }

        var (world, scheduler) = CreatePipeline(cameraEntity.Value, asset.Passes);

        RenderFramer.Enqueue(entity, () => {
            stateEntity.Get<RenderPipelineState>() = new RenderPipelineState {
                CameraEntity = cameraEntity.Value,
                World = world,
                Scheduler = scheduler
            };
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

    public override CancellationToken? DestroyState(in EntityRef entity, in RenderPipeline asset, in EntityRef stateEntity)
    {
        var source = new CancellationTokenSource();
        stateEntity.Hang(e => {
            e.Get<RenderPipelineState>().World.Dispose();
            e.Dispose();
        });
        RenderFramer.Enqueue(entity, source.Cancel);
        return source.Token;
    }
}