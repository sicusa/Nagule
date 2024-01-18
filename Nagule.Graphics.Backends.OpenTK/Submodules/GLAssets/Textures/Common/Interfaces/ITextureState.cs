namespace Nagule.Graphics.Backends.OpenTK;

public interface ITextureState : IAssetState
{
    bool MipmapEnabled { get; set; }
    TextureHandle Handle { get; set; }
}