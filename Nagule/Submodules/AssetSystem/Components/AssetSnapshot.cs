namespace Nagule;

public record struct AssetSnapshot<TAsset>
    where TAsset : struct
{
    public TAsset Asset;

    public AssetSnapshot(in TAsset asset)
    {
        Asset = asset;
    }
}