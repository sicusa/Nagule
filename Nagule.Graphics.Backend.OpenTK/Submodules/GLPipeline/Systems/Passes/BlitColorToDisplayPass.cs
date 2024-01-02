namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class BlitColorToDisplayPass : RenderPassSystemBase
{
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

        var primaryWindow = world.GetAddon<PrimaryWindow>();
        var framebuffer = Pipeline.GetAddon<Framebuffer>();

        var blitProgramEntity = GLSLProgram.CreateEntity(
            world, s_blitProgramAsset, AssetLife.Persistent);

        RenderFrame.Start(() => {
            ref var blitProgramState = ref blitProgramEntity.GetState<GLSLProgramState>();
            if (!blitProgramState.Loaded) { return NextFrame; }

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);
            GL.UseProgram(blitProgramState.Handle.Handle);

            var window = primaryWindow.Entity.Get<Window>();
            var (width, height) = window.PhysicalSize;
            GL.Viewport(0, 0, width, height);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, framebuffer.ColorHandle.Handle);
            GL.Uniform1i(0, 0);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.Disable(EnableCap.DepthTest);
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
            GL.Enable(EnableCap.DepthTest);

            GL.BindVertexArray(0);
            return NextFrame;
        });
    }
}