namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;
using Nagule;

using TextureWrapMode = global::OpenTK.Graphics.OpenGL.TextureWrapMode;
using TextureMagFilter = global::OpenTK.Graphics.OpenGL.TextureMagFilter;
using TextureMinFilter = global::OpenTK.Graphics.OpenGL.TextureMinFilter;

public class RenderTargetManager : ResourceManagerBase<RenderTarget, RenderTargetData, RenderTargetResource>, IWindowResizeListener
{
    private int _windowWidth;
    private int _windowHeight;

    private DrawBufferMode[] _transparentDrawModes = {
        DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1
    };

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

            GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
            GL.BufferData(BufferTargetARB.UniformBuffer, 8, IntPtr.Zero, BufferUsageARB.DynamicDraw);

            if (resource.AutoResizeByWindow) {
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

        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 4, data.Width);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 4, 4, data.Height);

        // Initialize color framebuffer

        data.ColorTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.ColorTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        data.DepthTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.DepthTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent32, width, height, 0, PixelFormat.DepthComponent, PixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        GL.GenerateMipmap(TextureTarget.Texture2d);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.ColorFramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, data.ColorTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, data.DepthTextureHandle, 0);

        // Initialize transparency buffer

        data.TransparencyAccumTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.TransparencyAccumTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        data.TransparencyAlphaTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.TransparencyAlphaTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, width, height, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.TransparencyFramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, data.TransparencyAccumTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, data.TransparencyAlphaTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, data.DepthTextureHandle, 0);
        GL.DrawBuffers(_transparentDrawModes);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
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