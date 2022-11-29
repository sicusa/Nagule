namespace Nagule.Backend.OpenTK.Graphics;

public struct RenderTargetData : IPooledComponent
{
    public int UniformBufferHandle;
    public int Width;
    public int Height;

    public int ColorFramebufferHandle;
    public int ColorTextureHandle;
    public int DepthTextureHandle;

    public int TransparencyFramebufferHandle;
    public int TransparencyAccumTextureHandle;
    public int TransparencyAlphaTextureHandle;
}