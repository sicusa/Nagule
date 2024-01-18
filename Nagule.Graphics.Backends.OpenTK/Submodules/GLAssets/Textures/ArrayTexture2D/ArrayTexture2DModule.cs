namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public record struct ArrayTexture2DState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;

    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }
}

[NaAssetModule<RArrayTexture2D, ArrayTexture2DState>(typeof(TextureManagerBase<,,>))]
internal partial class ArrayTexture2DModule;