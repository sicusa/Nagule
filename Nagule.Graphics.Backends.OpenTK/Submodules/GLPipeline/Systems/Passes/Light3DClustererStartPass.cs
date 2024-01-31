namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DClustererStartPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var cameraStateEntity = ref CameraState.Get<Camera3DState>();
        if (!cameraStateEntity.Loaded) { return; }

        var clusterer = world.AcquireAddon<Light3DClusterer>();
        if (cameraStateEntity.ParametersVersion != clusterer.CameraParametersVersion) {
            clusterer.UpdateClusters(cameraStateEntity);
        }
        clusterer.StartClusterTasks(CameraState);
    }
}