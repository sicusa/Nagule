namespace Nagule;

public interface ILoadableAsset<TAsset> : IAsset
    where TAsset : ILoadableAsset<TAsset>
{
    static abstract TAsset Load(Stream stream, string? name = null);
}

public interface ILoadableAsset<TAsset, TOptions> : ILoadableAsset<TAsset>
    where TAsset : ILoadableAsset<TAsset, TOptions>
{
    static abstract TAsset Load(Stream stream, TOptions options, string? name = null);
}