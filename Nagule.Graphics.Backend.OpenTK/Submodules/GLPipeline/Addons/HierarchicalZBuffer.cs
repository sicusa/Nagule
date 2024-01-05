namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class HierarchicalZBuffer : IAddon
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public TextureHandle TextureHandle { get; private set; }

    public void Load(int width, int height)
    {
        Width = width;
        Height = height;
        TextureHandle = new(GL.GenTexture());
        Resize(width, height);
    }

    public void Resize(int width, int height)
    {
        Width = width;
        Height = height;

        GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.DepthComponent24, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.NearestMipmapNearest);
        GL.GenerateMipmap(TextureTarget.Texture2d);
        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    public void Unload()
    {
        GL.DeleteTexture(TextureHandle.Handle);
        TextureHandle = TextureHandle.Zero;
    }
}