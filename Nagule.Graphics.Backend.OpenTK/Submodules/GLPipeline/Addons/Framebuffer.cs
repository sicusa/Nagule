namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class Framebuffer : IAddon
{
    public BufferHandle UniformBufferHandle { get; private set; }

    public FramebufferHandle Handle => _handle;
    public TextureHandle ColorHandle => _colorHandle;
    public TextureHandle DepthHandle => _depthHandle;

    public VertexArrayHandle EmptyVertexArray { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    private GLSync _sync;

    private FramebufferHandle _handle;
    private TextureHandle _colorHandle;

    private FramebufferHandle _anotherHandle;
    private TextureHandle _anotherColorHandle;

    private TextureHandle _depthHandle;

    public void Load(int width, int height)
    {
        Width = width;
        Height = height;

        UniformBufferHandle = new(GL.GenBuffer());
        EmptyVertexArray = new(GL.GenVertexArray());

        GL.BindBuffer(BufferTargetARB.UniformBuffer, UniformBufferHandle.Handle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 8, (Width, Height), BufferUsageARB.DynamicDraw);
        GL.BindBuffer(BufferTargetARB.UniformBuffer, 0);

        _handle = new(GL.GenFramebuffer());
        _anotherHandle = new(GL.GenFramebuffer());

        GenerateDepthBuffer();
        InitializeFramebuffer(Handle, out _colorHandle);
        InitializeFramebuffer(_anotherHandle, out _anotherColorHandle);
    }

    public void Unload()
    {
        GL.DeleteBuffer(UniformBufferHandle.Handle);

        GL.DeleteFramebuffer(Handle.Handle);
        GL.DeleteFramebuffer(_anotherHandle.Handle);

        GL.DeleteTexture(ColorHandle.Handle);
        GL.DeleteTexture(DepthHandle.Handle);
        GL.DeleteTexture(_anotherColorHandle.Handle);
    } 

    public void FenceSync() => GLUtils.FenceSync(ref _sync);
    public void WaitSync() => GLUtils.WaitSync(_sync);

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
        GL.DeleteTexture(_anotherColorHandle.Handle);

        GenerateDepthBuffer();
        InitializeFramebuffer(Handle, out _colorHandle);
        InitializeFramebuffer(_anotherHandle, out _anotherColorHandle);
    }

    public void Swap()
    {
        static void DoSwap<T>(ref T a, ref T b)
            => (b, a) = (a, b);
        
        DoSwap(ref _handle, ref _anotherHandle);
        DoSwap(ref _colorHandle, ref _anotherColorHandle);
    }

    private void GenerateDepthBuffer()
    {
        _depthHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, _depthHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.DepthComponent24, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);
    }

    private unsafe void InitializeFramebuffer(FramebufferHandle handle, out TextureHandle colorTex)
    {
        int currentFramebuffer;
        GL.GetIntegerv((GetPName)0x8ca6, &currentFramebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle.Handle);

        colorTex = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, colorTex.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.Rgba16f, Width, Height, 0, GLPixelFormat.Rgba, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Linear);

        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, colorTex.Handle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depthHandle.Handle, 0);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFramebuffer);
        GL.BindTexture(TextureTarget.Texture2d, 0);
    }
}