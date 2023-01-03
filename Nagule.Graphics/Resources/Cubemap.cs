namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public record Cubemap : ResourceBase
{
    public ImmutableDictionary<CubemapTextureTarget, Image> Images { get; init; }
        = ImmutableDictionary<CubemapTextureTarget, Image>.Empty;

    public TextureType Type { get; init; } = TextureType.Unknown;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.Repeat;
    public TextureWrapMode WrapW { get; init; } = TextureWrapMode.Repeat;

    public TextureMinFilter MinFilter { get; init; } = TextureMinFilter.Linear;
    public TextureMagFilter MaxFilter { get; init; } = TextureMagFilter.Linear;

    public Vector4 BorderColor { get; init; } = Vector4.Zero;
    public bool MipmapEnabled { get; init; } = true;

    public Cubemap WithImage(CubemapTextureTarget target, Image image)
        => this with { Images = Images.SetItem(target, image) };

    public Cubemap WithImages(params KeyValuePair<CubemapTextureTarget, Image>[] images)
        => this with { Images = Images.SetItems(images) };

    public Cubemap WithImages(IEnumerable<KeyValuePair<CubemapTextureTarget, Image>> images)
        => this with { Images = Images.SetItems(images) };
}