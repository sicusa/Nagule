namespace Nagule.Graphics.Backends.OpenTK;

public record struct ArrayTexture2DState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;

    public bool MipmapEnabled { get; set; }
    public TextureMinFilter MinFilter { get; set; }
    public TextureMagFilter MagFilter { get; set; }
    public TextureHandle Handle { get; set; }
}