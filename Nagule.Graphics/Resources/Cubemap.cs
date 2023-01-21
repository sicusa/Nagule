namespace Nagule.Graphics;

using System.Linq;
using System.Numerics;
using System.Collections.Immutable;

public record Cubemap : ResourceBase
{
    public ImmutableDictionary<CubemapFace, ImageBase> Images { get; init; }
        = ImmutableDictionary<CubemapFace, ImageBase>.Empty;

    public TextureType Type { get; init; } = TextureType.Diffuse;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.ClampToEdge;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.ClampToEdge;
    public TextureWrapMode WrapW { get; init; } = TextureWrapMode.ClampToEdge;

    public TextureMinFilter MinFilter { get; init; } = TextureMinFilter.Linear;
    public TextureMagFilter MaxFilter { get; init; } = TextureMagFilter.Linear;

    public Vector4 BorderColor { get; init; } = Vector4.Zero;
    public bool MipmapEnabled { get; init; } = false;

    public Cubemap WithImage(CubemapFace target, ImageBase image)
        => this with { Images = Images.SetItem(target, image) };

    public Cubemap WithImages(params KeyValuePair<CubemapFace, ImageBase>[] images)
        => this with { Images = Images.SetItems(images) };

    public Cubemap WithImages(IEnumerable<KeyValuePair<CubemapFace, ImageBase>> images)
        => this with { Images = Images.SetItems(images) };

    public Cubemap WithImages(params ImageBase[] images)
        => WithImages((IEnumerable<ImageBase>)images);

    public Cubemap WithImages(IEnumerable<ImageBase> images)
        => WithImages(
            Enumerable.Range(0, (int)CubemapFace.Count).Zip(images)
                .Select(t => KeyValuePair.Create((CubemapFace)t.First, t.Second)));
}