namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class FrameBeginPass : RenderPassBase
{
    private PrimaryWindow? _primaryWindow;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _primaryWindow = MainWorld.GetAddon<PrimaryWindow>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var framebuffer = world.AcquireAddon<PipelineFramebuffer>();
        var clearFlags = ClearFlags.Color | ClearFlags.Depth;

        ref var cameraState = ref CameraState.Get<Camera3DState>();
        if (cameraState.Loaded) {
            clearFlags = cameraState.ClearFlags;

            ref var renderSettingsState = ref cameraState.RenderSettingsState
                .Get<RenderSettingsState>();

            if (renderSettingsState.Loaded) {
                int width, height;
                var resolution = renderSettingsState.Resolution;
                if (resolution != null) {
                    (width, height) = resolution.Value;
                }
                else if (cameraState.TargetTextureState is EntityRef targetTexState) {
                    ref var texState = ref targetTexState.Get<RenderTexture2DState>();
                    width = texState.Width;
                    height = texState.Height;
                }
                else {
                    ref var window = ref _primaryWindow!.Entity.Get<Window>();
                    (width, height) = window.PhysicalSize;
                }
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

        framebuffer.Update(RenderFramer.Time);
    }
}