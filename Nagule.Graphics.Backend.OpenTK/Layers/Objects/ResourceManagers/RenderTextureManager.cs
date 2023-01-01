namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;
using Nagule;

using PixelFormat = Nagule.Graphics.PixelFormat;

public class RenderTextureManager
    : ResourceManagerBase<RenderTexture, RenderTextureData>, IWindowResizeListener, IRenderListener
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
    private float[] _tempBorderColor = new float[4];

    private ConcurrentQueue<(CommandType, Guid, RenderTexture)> _commandQueue = new();

    public void OnWindowResize(IContext context, int width, int height)
    {
        if (_windowWidth == width && _windowHeight == height) {
            return;
        }
        _windowWidth = width;
        _windowHeight = height;

        foreach (var id in context.Query<RenderTextureAutoResizeByWindow>()) {
            ref var data = ref context.Require<RenderTextureData>(id);
            data.Width = width;
            data.Height = height;
            var resource = context.Inspect<Resource<RenderTexture>>(id).Value!;
            _commandQueue.Enqueue((CommandType.Update, id, resource));
        }
    }

    protected override void Initialize(
        IContext context, Guid id, RenderTexture resource, ref RenderTextureData data, bool updating)
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

        data.Width = width;
        data.Height = height;

        _commandQueue.Enqueue(
            (updating ? CommandType.Reinitialize : CommandType.Initialize, id, resource));
    }

    protected override void Uninitialize(IContext context, Guid id, RenderTexture resource, in RenderTextureData data)
    {
        context.Remove<RenderTextureAutoResizeByWindow>(id);
        _commandQueue.Enqueue((CommandType.Uninitialize, id, resource));
    }

    public void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id, resource) = command;
            ref var data = ref context.Require<RenderTextureData>(id);

            switch (commandType) {
            case CommandType.Initialize:
                InitializeHandles(ref data);
                CreateTextures(context, id, resource, ref data);
                break;
            case CommandType.Reinitialize:
                DeleteTextures(in data);
                InitializeHandles(ref data);
                CreateTextures(context, id, resource, ref data);
                break;
            case CommandType.Update:
                DeleteTextures(in data);
                CreateTextures(context, id, resource, ref data);
                break;
            case CommandType.Uninitialize:
                DeleteTextures(in data);
                DeleteBuffers(in data);
                break;
            }
        }
    }

    private void InitializeHandles(ref RenderTextureData data)
    {
        data.FramebufferHandle = GL.GenFramebuffer();
    }

    private void CreateTextures(
        IContext context, Guid id, RenderTexture resource, ref RenderTextureData data)
    {
        int width = data.Width;
        int height = data.Height;

        data.TextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, data.TextureHandle);

        InternalFormat format;

        switch (resource.PixelFormat) {
        case PixelFormat.Grey:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.R8,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Red,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.GreyAlpha:
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.Rg8,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rg,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlue:
            format = resource.Type switch {
                TextureType.Diffuse => InternalFormat.Srgb,
                _ => InternalFormat.Rgb
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgb,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        case PixelFormat.RedGreenBlueAlpha:
            format = resource.Type switch {
                TextureType.Diffuse => InternalFormat.SrgbAlpha,
                _ => InternalFormat.Rgb
            };
            GL.TexImage2D(
                TextureTarget.Texture2d, 0, format,
                width, height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte, IntPtr.Zero);
            break;
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureHelper.Cast(resource.WrapU));
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureHelper.Cast(resource.WrapV));
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureHelper.Cast(resource.MinFilter));
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureHelper.Cast(resource.MaxFilter));

        resource.BorderColor.CopyTo(_tempBorderColor);
        GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, _tempBorderColor);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, data.FramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, data.TextureHandle, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Zero);
    }

    private void DeleteTextures(in RenderTextureData data)
    {
        GL.DeleteTexture(data.TextureHandle);
    }

    private void DeleteBuffers(in RenderTextureData data)
    {
        GL.DeleteFramebuffer(data.FramebufferHandle);
    }
}