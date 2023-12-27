namespace Nagule.Graphics.Backend.OpenTK;

public record struct RenderTexture2DState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;

    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }

    public int Width;
    public int Height;
    public FramebufferHandle FramebufferHandle;
}