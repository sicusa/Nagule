namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics.OpenGL4;

using Nagule.Graphics;
using Nagule;

using TextureWrapMode = Nagule.Graphics.TextureWrapMode;
using TextureMagFilter = Nagule.Graphics.TextureMagFilter;
using TextureMinFilter = Nagule.Graphics.TextureMinFilter;

public class RenderTargetManager : ResourceManagerBase<RenderTarget, RenderTargetData, RenderTargetResource>, IWindowResizeListener
{
    private int _windowWidth;
    private int _windowHeight;
    private DrawBuffersEnum[] _transparentDraw = { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 };

    public void OnWindowResize(IContext context, int width, int height)
    {
        if (_windowWidth == width && _windowHeight == height) {
            return;
        }
        foreach (var id in context.Query<RenderTargetAutoResizeByWindow>()) {
            ref var framebuffer = ref context.UnsafeInspect<RenderTarget>(id);
            ref var data = ref context.Require<RenderTargetData>(id);
            DeleteTextures(in data);
            UpdateData(context, id, ref framebuffer, ref data, width, height);
        }
        _windowWidth = width;
        _windowHeight = height;
    }

    protected override void Initialize(
        IContext context, Guid id, ref RenderTarget framebuffer, ref RenderTargetData data, bool updating)
    {
        var resource = framebuffer.Resource;
        int width = resource.Width;
        int height = resource.Height;

        if (updating) {
            DeleteTextures(in data);
        }
        else {
            data.UniformBufferHandle = GL.GenBuffer();
            data.ColorFramebufferHandle = GL.GenFramebuffer();
            data.TransparencyFramebufferHandle = GL.GenFramebuffer();

            GL.BindBuffer(BufferTarget.UniformBuffer, data.UniformBufferHandle);
            GL.BufferData(BufferTarget.UniformBuffer, 8, IntPtr.Zero, BufferUsageHint.StaticDraw);

            if (resource.AutoResizeByWindow) {
                Console.WriteLine("autoresize");
                context.Acquire<RenderTargetAutoResizeByWindow>(id);
                width = _windowWidth;
                height = _windowHeight;
            }
        }
        UpdateData(context, id, ref framebuffer, ref data, width, height);
    }

    private void UpdateData(
        IContext context, Guid id, ref RenderTarget framebuffer, ref RenderTargetData data, int width, int height)
    {
        data.Width = width;
        data.Height = height;

        GL.BindBuffer(BufferTarget.UniformBuffer, data.UniformBufferHandle);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, 4, ref data.Width);
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero + 4, 4, ref data.Height);

        // Initialize color framebuffer

        data.ColorTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, data.ColorTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        data.DepthTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, data.DepthTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent24, width, height, 0, PixelFormat.DepthComponent, PixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.ColorFramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, data.ColorTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, data.DepthTextureHandle, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        // Initialize transparency buffer

        data.TransparencyAccumTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, data.TransparencyAccumTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        data.TransparencyAlphaTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, data.TransparencyAlphaTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, width, height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.TransparencyFramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, data.TransparencyAccumTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, data.TransparencyAlphaTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, data.DepthTextureHandle, 0);
        GL.DrawBuffers(2, _transparentDraw);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
    }

    protected override void Uninitialize(IContext context, Guid id, in RenderTarget framebuffer, in RenderTargetData data)
    {
        DeleteTextures(in data);
        context.Remove<RenderTargetAutoResizeByWindow>(id);

        GL.DeleteBuffer(data.UniformBufferHandle);
        GL.DeleteFramebuffer(data.ColorFramebufferHandle);
        GL.DeleteFramebuffer(data.TransparencyFramebufferHandle);
    }

    private void DeleteTextures(in RenderTargetData data)
    {
        GL.DeleteTexture(data.ColorTextureHandle);
        GL.DeleteTexture(data.DepthTextureHandle);
        GL.DeleteTexture(data.TransparencyAccumTextureHandle);
        GL.DeleteTexture(data.TransparencyAlphaTextureHandle);
    }
}