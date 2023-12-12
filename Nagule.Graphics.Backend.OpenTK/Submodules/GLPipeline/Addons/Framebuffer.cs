namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class Framebuffer : IAddon
{
    public FramebufferHandle Handle { get; private set; }
    public BufferHandle UniformBufferHandle { get; private set; }

    public TextureHandle ColorHandle { get; private set; }
    public TextureHandle DepthHandle { get; private set; }

    public VertexArrayHandle EmptyVertexArray { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    private GLSync _sync;
    private GLSync _sync2;

    public void Load(int width, int height)
    {
        Handle = new(GL.GenFramebuffer());
        UniformBufferHandle = new(GL.GenBuffer());
        
        Width = width;
        Height = height;

        EmptyVertexArray = new(GL.GenVertexArray());

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle.Handle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 8, (Width, Height), BufferUsageARB.DynamicDraw);
        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        GenerateBuffers();
    }

    public void Unload()
    {
        GL.DeleteFramebuffer(Handle.Handle);
        GL.DeleteBuffer(UniformBufferHandle.Handle);
        GL.DeleteTexture(ColorHandle.Handle);
        GL.DeleteTexture(DepthHandle.Handle);
    } 

    public void FenceSync() => GLUtils.FenceSync(ref _sync);
    public void WaitSync() => GLUtils.WaitSync(_sync);

    public void FenceSync2() => GLUtils.FenceSync(ref _sync2);
    public void WaitSync2() => GLUtils.WaitSync(_sync2);

    public unsafe void Resize(int width, int height)
    {
        Width = width;
        Height = height;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle.Handle);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 4, width);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 4, 4, height);
        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        GL.DeleteTexture(ColorHandle.Handle);
        GL.DeleteTexture(DepthHandle.Handle);

        GenerateBuffers();
    }

    private unsafe void GenerateBuffers()
    {
        int currentFramebuffer;
        GL.GetIntegerv((GetPName)0x8ca6, &currentFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle.Handle);

        ColorHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, ColorHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.Rgba16f, Width, Height, 0, GLPixelFormat.Rgba, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Linear);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, ColorHandle.Handle, 0);

        DepthHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, DepthHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.DepthComponent24, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, DepthHandle.Handle, 0);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFramebuffer);
        GL.BindTexture(TextureTarget.Texture2d, 0);
    }
}