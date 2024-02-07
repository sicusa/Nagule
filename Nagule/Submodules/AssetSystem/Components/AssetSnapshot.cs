namespace Nagule;

public record struct AssetSnapshot<TAsset>
{
    public TAsset Asset;

    public AssetSnapshot(in TAsset asset)
    {
        Asset = asset;
    }
}