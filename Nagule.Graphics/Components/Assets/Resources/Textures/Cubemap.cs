namespace Nagule.Graphics;

using System.Linq;
using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Cubemap))]
[NaAsset]
public record RCubemap : RTextureBase
{
    public static RCubemap Empty { get; } = new();

    public ImmutableDictionary<CubemapFace, RImageBase> Images { get; init; }
        = ImmutableDictionary<CubemapFace, RImageBase>.Empty;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.ClampToEdge;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.ClampToEdge;
    public TextureWrapMode WrapW { get; init; } = TextureWrapMode.ClampToEdge;

    public RCubemap()
    {
        IsMipmapEnabled = false;
    }

    public RCubemap WithImage(CubemapFace target, RImageBase image)
        => this with { Images = Images.SetItem(target, image) };

    public RCubemap WithImages(params KeyValuePair<CubemapFace, RImageBase>[] images)
        => this with { Images = Images.SetItems(images) };

    public RCubemap WithImages(IEnumerable<KeyValuePair<CubemapFace, RImageBase>> images)
        => this with { Images = Images.SetItems(images) };

    public RCubemap WithImages(params RImageBase[] images)
        => WithImages((IEnumerable<RImageBase>)images);

    public RCubemap WithImages(IEnumerable<RImageBase> images)
        => WithImages(
            Enumerable.Range(0, (int)CubemapFace.Count).Zip(images)
                .Select(t => KeyValuePair.Create((CubemapFace)t.First, t.Second)));
}