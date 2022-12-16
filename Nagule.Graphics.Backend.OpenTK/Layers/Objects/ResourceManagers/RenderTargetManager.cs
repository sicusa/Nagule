namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;
using Nagule;

using TextureWrapMode = global::OpenTK.Graphics.OpenGL.TextureWrapMode;
using TextureMagFilter = global::OpenTK.Graphics.OpenGL.TextureMagFilter;
using TextureMinFilter = global::OpenTK.Graphics.OpenGL.TextureMinFilter;

public class RenderTargetManager
    : ResourceManagerBase<RenderTarget, RenderTargetData, RenderTargetResource>, IWindowResizeListener, IRenderListener
{
    private enum CommandType
    {
        Initialize,
        Reinitialize,
        Update,
        Uninitialize
    }

    private int _windowWidth;
    private int _windowHeight;

    private ConcurrentQueue<(CommandType, Guid)> _commandQueue = new();

    private DrawBufferMode[] _transparentDrawModes = {
        DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1
    };

    public void OnWindowResize(IContext context, int width, int height)
    {
        if (_windowWidth == width && _windowHeight == height) {
            return;
        }
        _windowWidth = width;
        _windowHeight = height;

        foreach (var id in context.Query<RenderTargetAutoResizeByWindow>()) {
            ref var data = ref context.Require<RenderTargetData>(id);
            data.Width = width;
            data.Height = height;
            _commandQueue.Enqueue((CommandType.Update, id));
        }
    }

    protected override void Initialize(
        IContext context, Guid id, ref RenderTarget framebuffer, ref RenderTargetData data, bool updating)
    {
        var resource = framebuffer.Resource;
        int width = resource.Width;
        int height = resource.Height;

        if (resource.AutoResizeByWindow) {
            context.Acquire<RenderTargetAutoResizeByWindow>(id);
            width = _windowWidth;
            height = _windowHeight;
        }
        else {
            context.Remove<RenderTargetAutoResizeByWindow>(id);
        }

        data.Width = width;
        data.Height = height;

        _commandQueue.Enqueue(
            (updating ? CommandType.Reinitialize : CommandType.Initialize, id));
    }

    protected override void Uninitialize(IContext context, Guid id, in RenderTarget framebuffer, in RenderTargetData data)
    {
        context.Remove<RenderTargetAutoResizeByWindow>(id);
        _commandQueue.Enqueue((CommandType.Uninitialize, id));
    }

    public void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;
            ref var data = ref context.Require<RenderTargetData>(id);

            switch (commandType) {
            case CommandType.Initialize:
                InitializeHandles(ref data);
                CreateTextures(context, id, ref data);
                break;
            case CommandType.Reinitialize:
                DeleteTextures(in data);
                InitializeHandles(ref data);
                CreateTextures(context, id, ref data);
                break;
            case CommandType.Update:
                DeleteTextures(in data);
                CreateTextures(context, id, ref data);
                break;
            case CommandType.Uninitialize:
                DeleteTextures(in data);
                DeleteBuffers(in data);
                break;
            }
        }
    }

    private void InitializeHandles(ref RenderTargetData data)
    {
        data.UniformBufferHandle = GL.GenBuffer();
        data.ColorFramebufferHandle = GL.GenFramebuffer();
        data.TransparencyFramebufferHandle = GL.GenFramebuffer();

        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 12, IntPtr.Zero, BufferUsageARB.DynamicDraw);
    }

    private void CreateTextures(
        IContext context, Guid id, ref RenderTargetData data)
    {
        int width = data.Width;
        int height = data.Height;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 4, width);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 4, 4, height);

        data.ColorTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.ColorTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        data.DepthTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.DepthTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent32, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, PixelType.UnsignedInt, IntPtr.Zero);
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
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        data.TransparencyRevealTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.TransparencyRevealTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Red, PixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.TransparencyFramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, data.TransparencyAccumTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, data.TransparencyRevealTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, data.DepthTextureHandle, 0);
        GL.DrawBuffers(_transparentDrawModes);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
    }

    private void DeleteTextures(in RenderTargetData data)
    {
        GL.DeleteTexture(data.ColorTextureHandle);
        GL.DeleteTexture(data.DepthTextureHandle);
        GL.DeleteTexture(data.TransparencyAccumTextureHandle);
        GL.DeleteTexture(data.TransparencyRevealTextureHandle);
    }

    private void DeleteBuffers(in RenderTargetData data)
    {
        GL.DeleteBuffer(data.UniformBufferHandle);
        GL.DeleteFramebuffer(data.ColorFramebufferHandle);
        GL.DeleteFramebuffer(data.TransparencyFramebufferHandle);
    }
}