namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics.OpenGL4;

using Nagule.Graphics;

using TextureWrapMode = Nagule.Graphics.TextureWrapMode;
using TextureMagFilter = Nagule.Graphics.TextureMagFilter;
using TextureMinFilter = Nagule.Graphics.TextureMinFilter;

public class TextureManager : ResourceManagerBase<Texture, TextureData, TextureResource>
{
    protected override void Initialize(
        IContext context, Guid id, ref Texture texture, ref TextureData data, bool updating)
    {
        if (updating) {
            GL.DeleteTexture(data.Handle);
        }

        data.Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, data.Handle);

        var resource = texture.Resource;
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, Cast(resource.WrapU));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Cast(resource.WrapV));

        if (resource.BorderColor != null) {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, resource.BorderColor);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, Cast(resource.MinFilter));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, Cast(resource.MaxFilter));

        var image = resource.Image ?? ImageResource.Hint;
        GL.TexImage2D(
            TextureTarget.Texture2D, 0, PixelInternalFormat.SrgbAlpha,
            image.Width, image.Height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, image.Bytes);

        if (resource.MipmapEnabled) {
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    protected override void Uninitialize(IContext context, Guid id, in Texture texture, in TextureData data)
    {
        GL.DeleteTexture(data.Handle);
    }

    private int Cast(TextureWrapMode mode)
        => (int)(mode switch {
            TextureWrapMode.ClampToBorder => global::OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToBorder,
            TextureWrapMode.ClampToEdge => global::OpenTK.Graphics.OpenGL4.TextureWrapMode.ClampToEdge,
            TextureWrapMode.MirroredRepeat => global::OpenTK.Graphics.OpenGL4.TextureWrapMode.MirroredRepeat,
            TextureWrapMode.Repeat => global::OpenTK.Graphics.OpenGL4.TextureWrapMode.Repeat,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    private int Cast(TextureMinFilter filter)
        => (int)(filter switch {
            TextureMinFilter.Linear => global::OpenTK.Graphics.OpenGL4.TextureMinFilter.Linear,
            TextureMinFilter.LinearMipmapLinear => global::OpenTK.Graphics.OpenGL4.TextureMinFilter.LinearMipmapLinear,
            TextureMinFilter.LinearMipmapNearest => global::OpenTK.Graphics.OpenGL4.TextureMinFilter.LinearMipmapNearest,
            TextureMinFilter.Nearest => global::OpenTK.Graphics.OpenGL4.TextureMinFilter.Nearest,
            TextureMinFilter.NearestMipmapLinear => global::OpenTK.Graphics.OpenGL4.TextureMinFilter.NearestMipmapLinear,
            TextureMinFilter.NearestMipmapNearest => global::OpenTK.Graphics.OpenGL4.TextureMinFilter.NearestMipmapNearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });

    private int Cast(TextureMagFilter filter)
        => (int)(filter switch {
            TextureMagFilter.Linear => global::OpenTK.Graphics.OpenGL4.TextureMagFilter.Linear,
            TextureMagFilter.Nearest => global::OpenTK.Graphics.OpenGL4.TextureMagFilter.Nearest,
            _ => throw new NotSupportedException("Invalid texture wrap mode")
        });
}