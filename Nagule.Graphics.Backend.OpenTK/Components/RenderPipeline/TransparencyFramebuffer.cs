namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

public struct TransparencyFramebuffer : IRenderPipelineComponent
{
    public FramebufferHandle FramebufferHandle;
    public TextureHandle AccumTextureHandle;
    public TextureHandle RevealTextureHandle;
}