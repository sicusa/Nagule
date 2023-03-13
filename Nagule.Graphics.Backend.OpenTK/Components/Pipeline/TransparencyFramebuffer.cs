namespace Nagule.Graphics.Backend.OpenTK;

public struct TransparencyFramebuffer : IRenderPipelineComponent
{
    public FramebufferHandle FramebufferHandle;
    public TextureHandle AccumTextureHandle;
    public TextureHandle RevealTextureHandle;
}