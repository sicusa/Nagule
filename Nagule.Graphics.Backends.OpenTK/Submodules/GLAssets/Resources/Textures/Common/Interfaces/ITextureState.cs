namespace Nagule.Graphics.Backends.OpenTK;

public interface ITextureState : IAssetState
{
    TextureHandle Handle { get; set; }
    TextureMinFilter MinFilter { get; set; }
    TextureMagFilter MagFilter { get; set; }
    bool IsMipmapEnabled { get; set; }
}