using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public abstract class ColorRenderTargetBase : IRenderTarget
{
    private static readonly RGLSLProgram s_blitProgramAsset = 
        new RGLSLProgram {
            Name = "nagule.pipeline.blit_color"
        }
        .WithShaders(
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.common.blit_color.frag.glsl")),
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.quad.vert.glsl")))
        .WithParameter("ColorBuffer", ShaderParameterType.Texture2D);

    private EntityRef _blitProgramEntity;

    public abstract (int, int) ViewportSize { get; }

    public virtual void OnInitialize(World world, EntityRef cameraEntity)
    {
        _blitProgramEntity = world.AcquireAsset(s_blitProgramAsset, cameraEntity);
    }

    public virtual void OnUninitailize(World world, EntityRef cameraEntity)
    {
        cameraEntity.Unrefer(_blitProgramEntity);
    }

    public void Blit(IPipelineFramebuffer framebuffer)
    {
        ref var blitProgramState = ref _blitProgramEntity.GetState<GLSLProgramState>();
        if (!blitProgramState.Loaded) { return; }

        if (!PrepareBlit()) { return; }

        GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);
        GL.UseProgram(blitProgramState.Handle.Handle);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, framebuffer.ColorAttachmentHandle.Handle);
        GL.Uniform1i(0, 0);

        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.Disable(EnableCap.DepthTest);
        GL.DepthMask(false);
        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
        GL.Enable(EnableCap.DepthTest);
        GL.DepthMask(true);

        GL.BindVertexArray(0);
        GL.UseProgram(0);

        FinishBlit();
    }

    protected abstract bool PrepareBlit();
    protected abstract void FinishBlit();
}