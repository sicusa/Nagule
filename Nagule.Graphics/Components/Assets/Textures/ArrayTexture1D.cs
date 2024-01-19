namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(ArrayTexture1D))]
[NaAsset]
public record RArrayTexture1D : RTextureBase
{
    public static RArrayTexture2D Empty { get; } = new();

    public int? Capacity { get; init; }
    public ImmutableList<RImageBase> Images { get; init; } = [];
    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;

    public RArrayTexture1D WithImage(RImageBase image)
        => this with { Images = Images.Add(image) };

    public RArrayTexture1D WithImages(params RImageBase[] images)
        => this with { Images = Images.AddRange(images) };

    public RArrayTexture1D WithImages(IEnumerable<RImageBase> images)
        => this with { Images = Images.AddRange(images) };
}