namespace Nagule;


public static class FileAssets
{
    public static TAsset Load<TAsset>(AssetPath<TAsset> path)
        where TAsset : ILoadableAssetRecord<TAsset>
        => TAsset.Load(File.OpenRead(path), path);

    public static TAsset Load<TAsset, TOptions>(AssetPath<TAsset> path, TOptions options)
        where TAsset : ILoadableAssetRecord<TAsset, TOptions>
        => TAsset.Load(File.OpenRead(path), options, path);
}