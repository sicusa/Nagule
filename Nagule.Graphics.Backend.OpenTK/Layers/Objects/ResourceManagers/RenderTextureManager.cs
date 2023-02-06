namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule;
using Nagule.Graphics;

public class RenderTextureManager
    : ResourceManagerBase<RenderTexture>, IWindowResizeListener
{
    private class InitializeCommand : Command<InitializeCommand, RenderTarget>
    {
        public Guid RenderTextureId;
        public RenderTexture? Resource;
        public int Width;
        public int Height;

        public override Guid? Id => RenderTextureId;

        public override void Execute(ICommandHost host)
        {
            ref var data = ref host.Acquire<RenderTextureData>(RenderTextureId, out bool exists);
            data.Width = Width;
            data.Height = Height;

            ref var texData = ref host.Acquire<TextureData>(RenderTextureId);

            if (!exists) {
                data.FramebufferHandle = GL.GenFramebuffer();
            }
            else {
                GL.DeleteTexture(texData.Handle);
            }

            CreateTexture(Resource!, ref data, ref texData);
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid RenderTextureId;

        public override void Execute(ICommandHost host)
        {
            if (host.Remove<RenderTextureData>(RenderTextureId, out var data)) {
                GL.DeleteFramebuffer(data.FramebufferHandle);
                if (!host.Remove<TextureData>(RenderTextureId, out var texData)) {
                    Console.WriteLine("Internal error: texture data not found");
                }
                GL.DeleteTexture(texData.Handle);
            }
        }
    }

    private int _windowWidth;
    private int _windowHeight;

    private static float[] s_tempBorderColor = new float[4];

    public void OnWindowResize(IContext context, int width, int height)
    {
        if (_windowWidth == width && _windowHeight == height) {
            return;
        }
        _windowWidth = width;
        _windowHeight = height;

        foreach (var id in context.Query<RenderTextureAutoResizeByWindow>()) {
            var cmd = InitializeCommand.Create();
            cmd.RenderTextureId = id;
            cmd.Resource = context.Inspect<Resource<RenderTexture>>(id).Value!;
            cmd.Width = width;
            cmd.Height = height;
            context.SendCommandBatched(cmd);
        }
    }

    protected override void Initialize(
        IContext context, Guid id, RenderTexture resource, RenderTexture? prevResource)
    {
        int width = resource.Width;
        int height = resource.Height;

        if (resource.AutoResizeByWindow) {
            context.Acquire<RenderTextureAutoResizeByWindow>(id);
            width = _windowWidth;
            height = _windowHeight;
        }
        else {
            context.Remove<RenderTextureAutoResizeByWindow>(id);
        }
        
        var cmd = InitializeCommand.Create();
        cmd.RenderTextureId = id;
        cmd.Resource = resource;
        cmd.Width = width;
        cmd.Height = height;
        context.SendCommandBatched<RenderTarget>(cmd);
    }

    protected override void Uninitialize(IContext context, Guid id, RenderTexture resource)
    {
        context.Remove<RenderTextureAutoResizeByWindow>(id);

        var cmd = UninitializeCommand.Create();
        cmd.RenderTextureId = id;
        context.SendCommandBatched<RenderTarget>(cmd);
    }

    private static void CreateTexture(
        RenderTexture resource, ref RenderTextureData data, ref TextureData textureData)
    {
        int width = data.Width;
        int height = data.Height;

        textureData.Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, textureData.Handle);

        GLHelper.TexImage2D(resource.Type, resource.PixelFormat, width, height);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureHelper.Cast(resource.WrapU));
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureHelper.Cast(resource.WrapV));
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureHelper.Cast(resource.MinFilter));
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureHelper.Cast(resource.MaxFilter));

        resource.BorderColor.CopyTo(s_tempBorderColor);
        GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, s_tempBorderColor);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.FramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, textureData.Handle, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
    }
}