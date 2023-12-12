namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class FrameBeginPass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        ref var camera = ref Camera.Get<Camera3D>();
        var renderSettings = camera.RenderSettings;

        var primaryWindow = world.GetAddon<PrimaryWindow>();
        var cameraManager = world.GetAddon<Camera3DManager>();
        var renderSettingsManager = world.GetAddon<RenderSettingsManager>();
        var tex2DManager = world.GetAddon<Texture2DManager>();
        var framebuffer = Pipeline.AcquireAddon<Framebuffer>();

        RenderFrame.Start(() => {
            ref var cameraState = ref cameraManager.RenderStates.GetOrNullRef(Camera);
            if (Unsafe.IsNullRef(ref cameraState)) {
                return ShouldStop;
            }
            ref var renderSettingsState = ref renderSettingsManager.RenderStates.GetOrNullRef(
                cameraState.RenderSettingsEntity);
            if (Unsafe.IsNullRef(ref renderSettingsState)) {
                return ShouldStop;
            }
            framebuffer.Load(renderSettingsState.Width, renderSettingsState.Height);
            return true;
        });

        RenderFrame.Start(() => {
            ref var cameraState = ref cameraManager.RenderStates.GetOrNullRef(Camera);
            if (Unsafe.IsNullRef(ref cameraState)) {
                return ShouldStop;
            }

            ref var renderSettingsState = ref renderSettingsManager.RenderStates.GetOrNullRef(
                cameraState.RenderSettingsEntity);
            if (Unsafe.IsNullRef(ref renderSettingsState)) {
                return ShouldStop;
            }

            int width = renderSettingsState.Width;
            int height = renderSettingsState.Height;
            if (width != framebuffer.Width || height != framebuffer.Height) {
                framebuffer.Resize(width, height);
            }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);
            GL.Viewport(0, 0, width, height);
            GLUtils.Clear(cameraState.ClearFlags);

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, framebuffer.UniformBufferHandle.Handle);
            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraState.Handle.Handle);
            return ShouldStop;
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