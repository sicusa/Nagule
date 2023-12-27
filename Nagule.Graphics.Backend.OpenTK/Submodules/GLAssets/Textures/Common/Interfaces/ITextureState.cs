namespace Nagule.Graphics.Backend.OpenTK;

public interface ITextureState : IAssetState
{
    bool MipmapEnabled { get; set; }
    TextureHandle Handle { get; set; }
}