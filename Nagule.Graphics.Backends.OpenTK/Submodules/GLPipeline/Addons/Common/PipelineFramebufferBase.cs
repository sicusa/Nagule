namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public abstract class PipelineFramebufferBase : IPipelineFramebuffer
{
    public abstract FramebufferHandle Handle { get; }
    public abstract TextureHandle ColorAttachmentHandle { get; }
    public abstract TextureHandle DepthAttachmentHandle { get; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public BufferHandle UniformBufferHandle { get; private set; }

    protected RenderPipelineInfo PipelineInfo { get; private set; } = null!;
    protected unsafe ref PipelineUniform Uniform => ref *(PipelineUniform*)_uniformPointer;

    private IntPtr _uniformPointer;

    public unsafe virtual void OnInitialize(World world)
    {
        PipelineInfo = world.GetAddon<RenderPipelineInfo>();

        Width = 512;
        Height = 512;

        UniformBufferHandle = new(GL.GenBuffer());

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle.Handle);
        _uniformPointer = GLUtils.InitializeBuffer(BufferTargetARB.UniformBuffer, PipelineUniform.MemorySize);

        Uniform.ViewportWidth = Width;
        Uniform.ViewportHeight = Height;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);
    }

    public virtual void OnUninitialize(World world)
    {
        GL.DeleteBuffer(UniformBufferHandle.Handle);
    }

    public virtual void Update(float time)
    {
        Uniform.Time = time;
    }

    public virtual void Resize(int width, int height)
    {
        Width = width;
        Height = height;

        Uniform.ViewportWidth = width;
        Uniform.ViewportHeight = height;
    }

    public abstract void SwapColorAttachments();
}