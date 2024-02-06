namespace Nagule.Graphics.Backends.OpenTK;

public interface ITextureState : IAssetState
{
    bool MipmapEnabled { get; set; }
    TextureMinFilter MinFilter { get; set; }
    TextureMagFilter MagFilter { get; set; }
    TextureHandle Handle { get; set; }
}