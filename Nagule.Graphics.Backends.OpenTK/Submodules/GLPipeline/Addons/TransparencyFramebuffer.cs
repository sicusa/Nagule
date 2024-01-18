namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class TransparencyFramebuffer : IAddon
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public FramebufferHandle Handle { get; private set; }
    public TextureHandle AccumTextureHandle { get; private set; }
    public TextureHandle RevealTextureHandle { get; private set; }

    private static readonly DrawBufferMode[] s_transparentDrawModes = [
        DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1
    ];

    public void OnInitialize(World world)
    {
        Handle = new(GL.GenFramebuffer());
        CreateTextures(world.GetAddon<Framebuffer>());
    }

    public void OnUninitialize(World world)
    {
        GL.DeleteTexture(AccumTextureHandle.Handle);
        GL.DeleteTexture(RevealTextureHandle.Handle);
        GL.DeleteFramebuffer(Handle.Handle);
    }

    public void Resize(Framebuffer framebuffer)
    {
        GL.DeleteTexture(AccumTextureHandle.Handle);
        GL.DeleteTexture(RevealTextureHandle.Handle);
        CreateTextures(framebuffer);
    }

    private void CreateTextures(Framebuffer framebuffer)
    {
        Width = framebuffer.Width;
        Height = framebuffer.Height;

        AccumTextureHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, AccumTextureHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.Rgba16f, Width, Height, 0, GLPixelFormat.Rgba, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);

        RevealTextureHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, RevealTextureHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.R16f, Width, Height, 0, GLPixelFormat.Red, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Handle.Handle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, AccumTextureHandle.Handle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, RevealTextureHandle.Handle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, framebuffer.DepthHandle.Handle, 0);
        GL.DrawBuffers(s_transparentDrawModes);

        GL.BindTexture(TextureTarget.Texture2d, 0);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);
    }
}