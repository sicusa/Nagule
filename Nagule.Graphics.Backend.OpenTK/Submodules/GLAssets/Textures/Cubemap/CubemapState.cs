
namespace Nagule.Graphics.Backend.OpenTK;

public record struct CubemapState : ITextureState
{
    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }
}