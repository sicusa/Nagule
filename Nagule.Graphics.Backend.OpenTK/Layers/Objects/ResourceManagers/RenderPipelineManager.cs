namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule;
using Nagule.Graphics;

using TextureWrapMode = global::OpenTK.Graphics.OpenGL.TextureWrapMode;
using TextureMagFilter = global::OpenTK.Graphics.OpenGL.TextureMagFilter;
using TextureMinFilter = global::OpenTK.Graphics.OpenGL.TextureMinFilter;

public class RenderPipelineManager : ResourceManagerBase<RenderPipeline>, IWindowResizeListener
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid RenderPipelineId;
        public int Width;
        public int Height;

        public override Guid? Id => RenderPipelineId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<RenderPipelineData>(RenderPipelineId, out bool exists);
            data.Width = Width;
            data.Height = Height;

            if (!exists) {
                CreateBuffers(ref data);
            }
            else {
                DeleteTextures(in data);
            }
            CreateTextures(ref data);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderPipelineId;

        public override void Execute(ICommandHost host)
        {
            if (!host.Remove<RenderPipelineData>(RenderPipelineId, out var data)) {
                return;
            }
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
            var cmd = InitializeCommand.Create();
            cmd.RenderPipelineId = id;
            cmd.Width = width;
            cmd.Height = height;
            context.SendCommandBatched(cmd);
        }
    }

    protected override void Initialize(
        IContext context, Guid id, RenderPipeline resource, RenderPipeline? prevResource)
    {
        int width = resource.Width;
        int height = resource.Height;

        if (resource.AutoResizeByWindow) {
            context.Acquire<RenderPipelineAutoResizeByWindow>(id);
            width = _windowWidth;
            height = _windowHeight;
        }
        else if (prevResource != null) {
            context.Remove<RenderPipelineAutoResizeByWindow>(id);
        }

        var cmd = InitializeCommand.Create();
        cmd.RenderPipelineId = id;
        cmd.Width = width;
        cmd.Height = height;
        context.SendCommandBatched(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, RenderPipeline resource)
    {
        context.Remove<RenderPipelineAutoResizeByWindow>(id);

        var cmd = UninitializeCommand.Create();
        cmd.RenderPipelineId = id;
        context.SendCommandBatched(cmd);
    }

    private static void CreateBuffers(ref RenderPipelineData data)
    {
        data.UniformBufferHandle = GL.GenBuffer();
        data.ColorFramebufferHandle = GL.GenFramebuffer();
        data.TransparencyFramebufferHandle = GL.GenFramebuffer();

        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferData(BufferTargetARB.UniformBuffer, 12, IntPtr.Zero, BufferUsageARB.DynamicDraw);
    }

    private static void CreateTextures(ref RenderPipelineData data)
    {
        int width = data.Width;
        int height = data.Height;

        GL.BindBuffer(BufferTargetARB.UniformBuffer, data.UniformBufferHandle);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero, 4, width);
        GL.BufferSubData(BufferTargetARB.UniformBuffer, IntPtr.Zero + 4, 4, height);

        data.ColorTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.ColorTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, global::OpenTK.Graphics.OpenGL.PixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        data.DepthTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.DepthTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent32, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, global::OpenTK.Graphics.OpenGL.PixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);

        data.HiZWidth = 512;
        data.HiZHeight = 256;

        data.HiZTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.HiZTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent32, data.HiZWidth, data.HiZHeight, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent, global::OpenTK.Graphics.OpenGL.PixelType.UnsignedInt, IntPtr.Zero);
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
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba, global::OpenTK.Graphics.OpenGL.PixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        data.TransparencyRevealTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.TransparencyRevealTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Red, global::OpenTK.Graphics.OpenGL.PixelType.HalfFloat, IntPtr.Zero);
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