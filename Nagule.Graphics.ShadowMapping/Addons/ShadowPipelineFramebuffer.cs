namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class ShadowPipelineFramebuffer : PipelineFramebufferBase
{
    public override FramebufferHandle Handle => _handle;

    public override TextureHandle ColorAttachmentHandle =>
        throw new NotSupportedException("ShadowPipelineFramebuffer does not support color attachment");

    public override TextureHandle DepthAttachmentHandle => _depthHandle;

    private FramebufferHandle _handle;
    private TextureHandle _depthHandle;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _handle = new(GL.GenFramebuffer());

        GenerateDepthBuffer();
        InitializeFramebuffer(_handle);
    }

    public override void OnUninitialize(World world)
    {
        base.OnUninitialize(world);

        GL.DeleteFramebuffer(_handle.Handle);
        GL.DeleteTexture(_depthHandle.Handle);
    }

    public override void Resize(int width, int height)
    {
        base.Resize(width, height);

        GL.DeleteTexture(_depthHandle.Handle);
        GenerateDepthBuffer();
        InitializeFramebuffer(_handle);
    }

    private unsafe void InitializeFramebuffer(FramebufferHandle handle)
    {
        int currentFramebuffer;
        GL.GetIntegerv((GetPName)0x8ca6, &currentFramebuffer);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, handle.Handle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depthHandle.Handle, 0);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, currentFramebuffer);
        GL.BindTexture(TextureTarget.Texture2d, 0);
    }

    private void GenerateDepthBuffer()
    {
        _depthHandle = new(GL.GenTexture());
        GL.BindTexture(TextureTarget.Texture2d, _depthHandle.Handle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, GLInternalFormat.DepthComponent, Width, Height, 0, GLPixelFormat.DepthComponent, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.Nearest);
    }

    public override void SwapColorAttachments()
        => throw new NotSupportedException("ShadowPipelineFramebuffer does not support swapping color attachments");
}