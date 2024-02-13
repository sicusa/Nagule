namespace Nagule.Graphics.Backends.OpenTK;

public record struct CubemapState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;

    public TextureHandle Handle { get; set; }
    public TextureMinFilter MinFilter { get; set; }
    public TextureMagFilter MagFilter { get; set; }
    public bool IsMipmapEnabled { get; set; }
}