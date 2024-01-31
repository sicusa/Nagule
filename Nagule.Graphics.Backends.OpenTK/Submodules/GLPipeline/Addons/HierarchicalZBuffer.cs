namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class HierarchicalZBuffer : IAddon
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int LevelCount { get; private set; }

    public FramebufferHandle FramebufferHandle { get; private set; }
    public TextureHandle TextureHandle { get; private set; }

    public void OnInitialize(World world)
    {
        var framebuffer = world.GetAddon<Framebuffer>();
        Load(framebuffer, 512, 256);
    }

    private void Load(Framebuffer framebuffer, int width, int height)
    {
        Width = width;
        Height = height;

        TextureHandle = new(GL.GenTexture());
        FramebufferHandle = new(GL.GenFramebuffer());

        Resize(framebuffer, width, height);
    }

    public void OnUninitialize(World world)
    {
        GL.DeleteTexture(TextureHandle.Handle);
        GL.DeleteFramebuffer(FramebufferHandle.Handle);
    }

    private void Resize(Framebuffer framebuffer, int width, int height)
    {
        Width = width;
        Height = height;
        LevelCount = 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(width, height)));

        GL.BindTexture(TextureTarget.Texture2d, TextureHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.DepthComponent24, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.NearestMipmapNearest);
        GL.GenerateMipmap(TextureTarget.Texture2d);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, FramebufferHandle.Handle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, TextureHandle.Handle, 0);
        GL.BindTexture(TextureTarget.Texture2d, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);
    }
}