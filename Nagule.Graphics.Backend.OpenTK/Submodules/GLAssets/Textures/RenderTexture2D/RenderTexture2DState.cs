namespace Nagule.Graphics.Backend.OpenTK;

public record struct RenderTexture2DState : ITextureState
{
    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }

    public int Width;
    public int Height;
    public FramebufferHandle FramebufferHandle;
}