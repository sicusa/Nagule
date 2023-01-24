namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

public struct RenderPipelineData : IPooledComponent
{
    public BufferHandle UniformBufferHandle;
    public int Width;
    public int Height;
    public int HiZWidth;
    public int HiZHeight;

    public FramebufferHandle ColorFramebufferHandle;
    public TextureHandle ColorTextureHandle;
    public TextureHandle DepthTextureHandle;
    public TextureHandle HiZTextureHandle;

    public FramebufferHandle TransparencyFramebufferHandle;
    public TextureHandle TransparencyAccumTextureHandle;
    public TextureHandle TransparencyRevealTextureHandle;
}