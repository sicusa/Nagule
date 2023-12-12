namespace Nagule.Graphics;

using System.Linq;
using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Cubemap))]
[NaguleAsset<Cubemap>]
public record CubemapAsset : TextureAssetBase
{
    public ImmutableDictionary<CubemapFace, ImageAssetBase> Images { get; init; }
        = ImmutableDictionary<CubemapFace, ImageAssetBase>.Empty;

    public TextureWrapMode WrapU { get; init; } = TextureWrapMode.ClampToEdge;
    public TextureWrapMode WrapV { get; init; } = TextureWrapMode.ClampToEdge;
    public TextureWrapMode WrapW { get; init; } = TextureWrapMode.ClampToEdge;

    public CubemapAsset()
    {
        MipmapEnabled = false;
        MinFilter = TextureMinFilter.Linear;
        MagFilter = TextureMagFilter.Linear;
    }

    public CubemapAsset WithImage(CubemapFace target, ImageAssetBase image)
        => this with { Images = Images.SetItem(target, image) };

    public CubemapAsset WithImages(params KeyValuePair<CubemapFace, ImageAssetBase>[] images)
        => this with { Images = Images.SetItems(images) };

    public CubemapAsset WithImages(IEnumerable<KeyValuePair<CubemapFace, ImageAssetBase>> images)
        => this with { Images = Images.SetItems(images) };

    public CubemapAsset WithImages(params ImageAssetBase[] images)
        => WithImages((IEnumerable<ImageAssetBase>)images);

    public CubemapAsset WithImages(IEnumerable<ImageAssetBase> images)
        => WithImages(
            Enumerable.Range(0, (int)CubemapFace.Count).Zip(images)
                .Select(t => KeyValuePair.Create((CubemapFace)t.First, t.Second)));
}