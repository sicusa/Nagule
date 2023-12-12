
namespace Nagule.Graphics.Backend.OpenTK;

public record struct Texture2DState : ITextureState
{
    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }
}