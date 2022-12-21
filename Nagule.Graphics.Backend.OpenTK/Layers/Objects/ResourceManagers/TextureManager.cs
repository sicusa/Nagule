namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Concurrent;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Nagule.Graphics;

using PixelFormat = Nagule.Graphics.PixelFormat;
using TextureWrapMode = Nagule.Graphics.TextureWrapMode;
using TextureMagFilter = Nagule.Graphics.TextureMagFilter;
using TextureMinFilter = Nagule.Graphics.TextureMinFilter;

public class TextureManager : ResourceManagerBase<Texture, TextureData>, IRenderListener
{
    private ConcurrentQueue<(bool, Guid, Texture)> _commandQueue = new();
    private float[] _tempBorderColor = new float[4];

    protected override void Initialize(
        IContext context, Guid id, Texture resource, ref TextureData data, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource, in data);
        }
        _commandQueue.Enqueue((true, id, resource));
    }

    protected override void Uninitialize(IContext context, Guid id, Texture resource, in TextureData data)
    {
        _commandQueue.Enqueue((false, id, resource));
    }

    public unsafe void OnRender(IContext context, float deltaTime)
    {
        while (_commandQueue.TryDequeue(out var command)) {
            var (commandType, id, resource) = command;
            ref var data = ref context.Require<TextureData>(id);

            if (commandType) {
                data.Handle = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2d, data.Handle);

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Cast(resource.WrapU));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Cast(resource.WrapV));

                resource.BorderColor.CopyTo(_tempBorderColor);
                GL.TexParameterf(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, _tempBorderColor);

                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Cast(resource.MinFilter));
                GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Cast(resource.MaxFilter));

                var image = resource.Image ?? Image.Hint;
                InternalFormat format;

                switch (image.PixelFormat) {
                case PixelFormat.Grey:
                    GL.TexImage2D(
                        TextureTarget.Texture2d, 0, InternalFormat.R8,
                        image.Width, image.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Red,
                        PixelType.UnsignedByte, image.Bytes.AsSpan());
                    break;
                case PixelFormat.GreyAlpha:
                    GL.TexImage2D(
                        TextureTarget.Texture2d, 0, InternalFormat.Rg8,
                        image.Width, image.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rg,
                        PixelType.UnsignedByte, image.Bytes.AsSpan());
                    break;
                case PixelFormat.RedGreenBlue:
                    format = resource.Type switch {
                        TextureType.Diffuse => InternalFormat.Srgb,
                        _ => InternalFormat.Rgb
                    };
                    GL.TexImage2D(
                        TextureTarget.Texture2d, 0, format,
                        image.Width, image.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgb,
                        PixelType.UnsignedByte, image.Bytes.AsSpan());
                    break;
                case PixelFormat.RedGreenBlueAlpha:
                    format = resource.Type switch {
                        TextureType.Diffuse => InternalFormat.SrgbAlpha,
                        _ => InternalFormat.Rgb
                    };
                    GL.TexImage2D(
                        TextureTarget.Texture2d, 0, format,
                        image.Width, image.Height, 0, global::OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                        PixelType.UnsignedByte, image.Bytes.AsSpan());
                    break;
                }
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