namespace Nagule.Graphics.Backends.OpenTK;

public record struct Texture2DState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;
    
    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }
}