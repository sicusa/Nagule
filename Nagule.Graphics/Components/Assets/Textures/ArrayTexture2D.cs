namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(ArrayTexture2D))]
[NaAsset]
public record RArrayTexture2D : RTextureBase
{
    public static RArrayTexture2D Empty { get; } = new();

    public int? Capacity { get; init; }
    public ImmutableList<RImageBase> Images { get; init; } = [];
    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;

    public RArrayTexture2D WithImage(RImageBase image)
        => this with { Images = Images.Add(image) };

    public RArrayTexture2D WithImages(params RImageBase[] images)
        => this with { Images = Images.AddRange(images) };

    public RArrayTexture2D WithImages(IEnumerable<RImageBase> images)
        => this with { Images = Images.AddRange(images) };
}