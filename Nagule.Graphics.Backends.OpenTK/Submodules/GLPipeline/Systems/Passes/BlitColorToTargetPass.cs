namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class BlitColorToTargetPass : RenderPassBase
{
    private PrimaryWindow? _primaryWindow;
    private EntityRef _blitProgramEntity;

    private static readonly RGLSLProgram s_blitProgramAsset = 
        new RGLSLProgram {
            Name = "nagule.pipeline.blit_color_to_display"
        }
        .WithShaders(
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.common.blit_color.frag.glsl")),
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.quad.vert.glsl")))
        .WithParameter("ColorBuffer", ShaderParameterType.Texture2D);

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _primaryWindow = MainWorld.GetAddon<PrimaryWindow>();
        _blitProgramEntity = GLSLProgram.CreateEntity(
            MainWorld, s_blitProgramAsset, AssetLife.Persistent);
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var blitProgramState = ref _blitProgramEntity.GetState<GLSLProgramState>();
        if (!blitProgramState.Loaded) { return; }

        var framebuffer = world.GetAddon<PipelineFramebuffer>();
        ref var cameraState = ref CameraState.Get<Camera3DState>();

        var targetTexHandle = TextureHandle.Zero;
        bool mipmapEnabled = false;

        if (cameraState.TargetTextureState is EntityRef targetTexStateEntity) {
            ref var targetTexState = ref targetTexStateEntity.Get<RenderTexture2DState>();
            if (!targetTexState.Loaded) {
                return;
            }

            targetTexHandle = targetTexState.Handle;
            mipmapEnabled = targetTexState.MipmapEnabled;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetTexState.FramebufferHandle.Handle);
            GL.Viewport(0, 0, targetTexState.Width, targetTexState.Height);
        }
        else {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            var window = _primaryWindow!.Entity.Get<Window>();
            var (width, height) = window.IsFullscreen ? window.Size : window.PhysicalSize;
            GL.Viewport(0, 0, width, height);
        }

        GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);
        GL.UseProgram(blitProgramState.Handle.Handle);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, framebuffer.ColorHandle.Handle);
        GL.Uniform1i(0, 0);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);

        GL.BindVertexArray(0);
        GL.UseProgram(0);

        if (mipmapEnabled) {
            GL.BindTexture(TextureTarget.Texture2d, targetTexHandle.Handle);
            GL.GenerateMipmap(TextureTarget.Texture2d);
            GL.BindTexture(TextureTarget.Texture2d, 0);
        }
    }
}