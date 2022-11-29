namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics.OpenGL4;

using Nagule.Graphics;

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
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)resource.WrapU);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)resource.WrapV);

        if (resource.BorderColor != null) {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, resource.BorderColor);
        }

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)resource.MinFitler);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)resource.MaxFitler);

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
}