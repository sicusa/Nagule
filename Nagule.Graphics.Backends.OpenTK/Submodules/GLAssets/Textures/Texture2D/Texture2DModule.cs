namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public record struct Texture2DState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;
    
    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }
}

[NaAssetModule<RTexture2D, Texture2DState>(typeof(TextureManagerBase<,,>))]
internal partial class Texture2DModule();