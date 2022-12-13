namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

public struct RenderTargetData : IPooledComponent
{
    public BufferHandle UniformBufferHandle;
    public int Width;
    public int Height;

    public FramebufferHandle ColorFramebufferHandle;
    public TextureHandle ColorTextureHandle;
    public TextureHandle DepthTextureHandle;

    public FramebufferHandle TransparencyFramebufferHandle;
    public TextureHandle TransparencyAccumTextureHandle;
    public TextureHandle TransparencyAlphaTextureHandle;
}