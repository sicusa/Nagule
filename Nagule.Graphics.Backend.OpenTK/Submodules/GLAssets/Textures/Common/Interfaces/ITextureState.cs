namespace Nagule.Graphics.Backend.OpenTK;

public interface ITextureState
{
    bool MipmapEnabled { get; set; }
    TextureHandle Handle { get; set; }
}