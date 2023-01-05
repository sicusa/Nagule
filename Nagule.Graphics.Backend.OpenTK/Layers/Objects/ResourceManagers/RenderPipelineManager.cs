namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule;
using Nagule.Graphics;

using TextureWrapMode = global::OpenTK.Graphics.OpenGL.TextureWrapMode;
using TextureMagFilter = global::OpenTK.Graphics.OpenGL.TextureMagFilter;
using TextureMinFilter = global::OpenTK.Graphics.OpenGL.TextureMinFilter;

public class RenderPipelineManager
    : ResourceManagerBase<RenderPipeline, RenderPipelineData>, IWindowResizeListener
{
    private class InitializeCommand : Command<InitializeCommand>
    {
        public Guid RenderPipelineId;
        public int Width;
        public int Height;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<RenderPipelineData>(RenderPipelineId);
            data.Width = Width;
            data.Height = Height;

            CreateBuffers(ref data);
            CreateTextures(context, RenderPipelineId, ref data);
        }
    }

    private class ReinitializeCommand : Command<ReinitializeCommand>
    {
        public Guid RenderPipelineId;
        public int Width;
        public int Height;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<RenderPipelineData>(RenderPipelineId);
            data.Width = Width;
            data.Height = Height;

            DeleteTextures(in data);
            CreateTextures(context, RenderPipelineId, ref data);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand>
    {
        public Guid RenderPipelineId;

        public override void Execute(IContext context)
        {
            ref var data = ref context.Require<RenderPipelineData>(RenderPipelineId);
            DeleteTextures(in data);
            DeleteBuffers(in data);
        }
    }

    private int _windowWidth;
    private int _windowHeight;

    private static DrawBufferMode[] s_transparentDrawModes = {
        DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1
    };

    public void OnWindowResize(IContext context, int width, int height)
    {
        if (_windowWidth == width && _windowHeight == height) {
            return;
        }
        _windowWidth = width;
        _windowHeight = height;

        foreach (var id in context.Query<RenderPipelineAutoResizeByWindow>()) {
            var cmd = Command<ReinitializeCommand>.Create();
            cmd.RenderPipelineId = id;
            cmd.Width = width;
            cmd.Height = height;
            context.SendCommand<RenderTarget>(cmd);
        }
    }

    protected override void Initialize(
        IContext context, Guid id, RenderPipeline resource, ref RenderPipelineData data, bool updating)
    {
        int width = resource.Width;
        int height = resource.Height;

        if (resource.AutoResizeByWindow) {
            context.Acquire<RenderPipelineAutoResizeByWindow>(id);
            width = _windowWidth;
            height = _windowHeight;
        }
        else if (updating) {
            context.Remove<RenderPipelineAutoResizeByWindow>(id);
        }

        if (updating) {
            var cmd = Command<ReinitializeCommand>.Create();
            cmd.RenderPipelineId = id;
            cmd.Width = width;
            cmd.Height = height;
            context.SendCommand<RenderTarget>(cmd);
        }
        else {
            var cmd = Command<InitializeCommand>.Create();
            cmd.RenderPipelineId = id;
            cmd.Width = width;
            cmd.Height = height;
            context.SendCommand<RenderTarget>(cmd);
        }
    }

    protected override void Uninitialize(IContext context, Guid id, RenderPipeline resource, in RenderPipelineData data)
    {
        context.Remove<RenderPipelineAutoResizeByWindow>(id);

        var cmd = Command<UninitializeCommand>.Create();
        cmd.RenderPipelineId = id;
        context.SendCommand<RenderTarget>(cmd);
    }

    private static void CreateBuffers(ref RenderPipelineData data)
    {
        data.UniformBufferHandle = GL.GenBuffer();
        data.ColorFramebufferHandle = GL.GenFramebuffer();
        data.TransparencyFramebufferHandle = GL.GenFramebuffer();

        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 12, IntPtr.Zero, BufferUsageARB.DynamicDraw);
    }

    private static void CreateTextures(
        IContext context, Guid id, ref RenderPipelineData data)
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
        GL.DrawBuffers(s_transparentDrawModes);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
    }

    private static void DeleteBuffers(in RenderPipelineData data)
    {
        GL.DeleteBuffer(data.UniformBufferHandle);
        GL.DeleteFramebuffer(data.ColorFramebufferHandle);
        GL.DeleteFramebuffer(data.TransparencyFramebufferHandle);
    }

    private static void DeleteTextures(in RenderPipelineData data)
    {
        GL.DeleteTexture(data.ColorTextureHandle);
        GL.DeleteTexture(data.DepthTextureHandle);
        GL.DeleteTexture(data.TransparencyAccumTextureHandle);
        GL.DeleteTexture(data.TransparencyRevealTextureHandle);
    }
}