namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class FrameBeginPass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        ref var camera = ref Camera.Get<Camera3D>();
        var cameraStateEntity = Camera.GetStateEntity();
        var renderSettings = camera.RenderSettings;

        var info = AddAddon<PipelineInfo>(Pipeline);
        info.CameraState = cameraStateEntity;
        info.MainWorld = world;

        Framebuffer? framebuffer = null;

        RenderFrame.Start(() => {
            framebuffer = AddAddon<Framebuffer>(Pipeline);
            return true;
        });

        RenderFrame.Start(() => {
            var clearFlags = ClearFlags.Color | ClearFlags.Depth;

            ref var cameraState = ref cameraStateEntity.Get<Camera3DState>();
            if (cameraState.Loaded) {
                clearFlags = cameraState.ClearFlags;

                ref var renderSettingsState = ref cameraState.RenderSettingsState
                    .Get<RenderSettingsState>();
                if (renderSettingsState.Loaded) {
                    int width = renderSettingsState.Width;
                    int height = renderSettingsState.Height;
                    if (framebuffer!.Width != width || framebuffer.Height != height) {
                        framebuffer.Resize(width, height);
                    }
                }
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer!.Handle.Handle);
            GL.Viewport(0, 0, framebuffer.Width, framebuffer.Height);
            GLUtils.Clear(clearFlags);

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, framebuffer.UniformBufferHandle.Handle);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraState.Handle.Handle);

            framebuffer.Update(world);
            return NextFrame;
        });
    }
}