namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DCullingPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var cameraState = ref CameraState.Get<Camera3DState>();
        if (!cameraState.Loaded) { return; }

        var clusterer = world.AcquireAddon<Light3DClusterer>();
        if (cameraState.ParametersVersion != clusterer.CameraParametersVersion) {
            clusterer.UpdateClusters(cameraState);
        }
        clusterer.ClusterVisibleLights(cameraState);
    }
}