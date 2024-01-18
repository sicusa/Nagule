namespace Nagule.Graphics.Backends.OpenTK;

public record struct CubemapState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;

    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }
}

[NaAssetModule<RCubemap, CubemapState>(typeof(TextureManagerBase<,,>))]
internal partial class CubemapModule;