namespace Nagule;

public readonly record struct AssetPath<TAssetRecord>(string Value)
    where TAssetRecord : IAssetRecord
{
    public static AssetPath<TAssetRecord> From(string value)
        => new(value);

    public static implicit operator string(AssetPath<TAssetRecord> path) => path.Value;
    public static implicit operator AssetPath<TAssetRecord>(string path) => new(path);
}