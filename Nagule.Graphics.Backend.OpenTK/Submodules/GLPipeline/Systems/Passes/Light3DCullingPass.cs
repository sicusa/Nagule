namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class Light3DCullingPass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        Light3DClustersBuffer? buffer = null;
        var cameraStateEntity = Camera.GetStateEntity();
        
        RenderFramer.Start(() => {
            ref var cameraState = ref cameraStateEntity.Get<Camera3DState>();
            if (!cameraState.Loaded) {
                return NextFrame;
            }
            buffer = Pipeline.AcquireAddon<Light3DClustersBuffer>();
            return true;
        });

        RenderFramer.Start(() => {
            if (buffer == null) {
                return NextFrame;
            }
            ref var cameraState = ref cameraStateEntity.Get<Camera3DState>();
            if (!cameraState.Loaded) { return NextFrame; }

            if (cameraState.ParametersVersion != buffer.CameraParametersVersion) {
                buffer.Update(cameraState);
            }
            buffer.CullLights(cameraState);
            return NextFrame;
        });
    }
}