namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class FrameBeginPass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        ref var camera = ref Camera.Get<Camera3D>();
        var renderSettings = camera.RenderSettings;

        var framebuffer = Pipeline.AcquireAddon<Framebuffer>();

        RenderFrame.Start(() => {
            framebuffer.Load(128, 128);
            return true;
        });

        RenderFrame.Start(() => {
            var clearFlags = ClearFlags.Color | ClearFlags.Depth;

            ref var cameraState = ref Camera.GetState<Camera3DState>();
            if (cameraState.Loaded) {
                clearFlags = cameraState.ClearFlags;

                ref var renderSettingsState = ref cameraState.RenderSettingsEntity
                    .GetState<RenderSettingsState>();
                if (renderSettingsState.Loaded) {
                    int width = renderSettingsState.Width;
                    int height = renderSettingsState.Height;
                    if (width != framebuffer.Width || height != framebuffer.Height) {
                        framebuffer.Resize(width, height);
                    }
                }
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);
            GL.Viewport(0, 0, framebuffer.Width, framebuffer.Height);
            GLUtils.Clear(clearFlags);

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, framebuffer.UniformBufferHandle.Handle);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraState.Handle.Handle);

            return NextFrame;
        });
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);

        var framebuffer = Pipeline.GetAddon<Framebuffer>();
        RenderFrame.Start(() => {
            framebuffer.Unload();
            return true;
        });
    }
}