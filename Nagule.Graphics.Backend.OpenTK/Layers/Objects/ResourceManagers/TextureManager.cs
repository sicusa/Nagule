namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

using TextureWrapMode = Nagule.Graphics.TextureWrapMode;
using TextureMagFilter = Nagule.Graphics.TextureMagFilter;
using TextureMinFilter = Nagule.Graphics.TextureMinFilter;

public class TextureManager : ResourceManagerBase<Texture, TextureData, TextureResource>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid)> _commandQueue = new();

    protected override void Initialize(
        IContext context, Guid id, ref Texture texture, ref TextureData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, in texture, in data);
        }
        _commandQueue.Enqueue((true, id));
    }

    protected override void Uninitialize(IContext context, Guid id, in Texture texture, in TextureData data)
    {
        _commandQueue.Enqueue((false, id));
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id) = command;
            ref var data = ref context.Require<TextureData>(id);

            if (commandType) {
                var resource = context.Inspect<Texture>(id).Resource;

                data.Handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2d, data.Handle);

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Cast(resource.WrapU));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Cast(resource.WrapV));

                if (resource.BorderColor != null) {
                    GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, resource.BorderColor);
                }

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Cast(resource.MinFilter));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Cast(resource.MaxFilter));

                var image = resource.Image ?? ImageResource.Hint;
                GL.TexImage2D(
                    TextureTarget.Texture2d, 0, InternalFormat.SrgbAlpha,
                    image.Width, image.Height, 0, PixelFormat.Rgba,
                    PixelType.UnsignedByte, image.Bytes!);

                if (resource.MipmapEnabled) {
                    GL.GenerateMipmap(TextureTarget.Texture2d);
                }

                GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Zero);
            }
            else {
                GL.DeleteTexture(data.Handle);
            }
        }
    }

    private int Cast(TextureWrapMode mode)
        => (int)(mode switch {
            TextureWrapMode.ClampToBorder => global::OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToBorder,
            TextureWrapMode.ClampToEdge => global::OpenTK.Graphics.OpenGL.TextureWrapMode.ClampToEdge,
            TextureWrapMode.MirroredRepeat => global::OpenTK.Graphics.OpenGL.TextureWrapMode.MirroredRepeat,
            TextureWrapMode.Repeat => global::OpenTK.Graphics.OpenGL.TextureWrapMode.Repeat,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    private int Cast(TextureMinFilter filter)
        => (int)(filter switch {
            TextureMinFilter.Linear => global::OpenTK.Graphics.OpenGL.TextureMinFilter.Linear,
            TextureMinFilter.LinearMipmapLinear => global::OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapLinear,
            TextureMinFilter.LinearMipmapNearest => global::OpenTK.Graphics.OpenGL.TextureMinFilter.LinearMipmapNearest,
            TextureMinFilter.Nearest => global::OpenTK.Graphics.OpenGL.TextureMinFilter.Nearest,
            TextureMinFilter.NearestMipmapLinear => global::OpenTK.Graphics.OpenGL.TextureMinFilter.NearestMipmapLinear,
            TextureMinFilter.NearestMipmapNearest => global::OpenTK.Graphics.OpenGL.TextureMinFilter.NearestMipmapNearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    private int Cast(TextureMagFilter filter)
        => (int)(filter switch {
            TextureMagFilter.Linear => global::OpenTK.Graphics.OpenGL.TextureMagFilter.Linear,
            TextureMagFilter.Nearest => global::OpenTK.Graphics.OpenGL.TextureMagFilter.Nearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });
}