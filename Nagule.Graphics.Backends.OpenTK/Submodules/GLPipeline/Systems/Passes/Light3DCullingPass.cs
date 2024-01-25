namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DCullingPass : RenderPassSystemBase
{
    private EntityRef _cameraState;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _cameraState = Camera.GetStateEntity();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var buffer = world.AcquireAddon<Light3DClustersBuffer>();

        ref var cameraState = ref _cameraState.Get<Camera3DState>();
        if (!cameraState.Loaded) { return; }

        if (cameraState.ParametersVersion != buffer.CameraParametersVersion) {
            buffer.Update(cameraState);
        }
        buffer.CullLights(cameraState);
    }
}