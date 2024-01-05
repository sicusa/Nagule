namespace Nagule;

public readonly record struct AssetPath<TAsset>(string Value)
    where TAsset : IAsset
{
    public static AssetPath<TAsset> From(string value)
        => new(value);

    public static implicit operator string(AssetPath<TAsset> path) => path.Value;
    public static implicit operator AssetPath<TAsset>(string path) => new(path);
}