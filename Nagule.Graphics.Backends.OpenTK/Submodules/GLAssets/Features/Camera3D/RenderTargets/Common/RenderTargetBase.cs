using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public abstract class RenderTargetBase : IRenderTarget
{
    public abstract (int, int) ViewportSize { get; }

    protected RenderFramer RenderFramer { get; private set; } = null!;

    public virtual void OnInitialize(World world, EntityRef cameraEntity)
    {
        RenderFramer = world.GetAddon<RenderFramer>();
    }

    public virtual void OnUninitialize(World world, EntityRef cameraEntity) {}

    public void Blit(ProgramHandle program, TextureHandle texture)
    {
        if (!PrepareBlit()) { return; }

        GL.BindVertexArray(GLUtils.EmptyVertexArray.Handle);
        GL.UseProgram(program.Handle);

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, texture.Handle);
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