namespace Nagule;

public interface ILoadableAssetRecord<TAssetRecord> : IAssetRecord
    where TAssetRecord : ILoadableAssetRecord<TAssetRecord>
{
    static abstract TAssetRecord Load(Stream stream, string? name = null);
}

public interface ILoadableAssetRecord<TAssetRecord, TOptions> : ILoadableAssetRecord<TAssetRecord>
    where TAssetRecord : ILoadableAssetRecord<TAssetRecord, TOptions>
{
    static abstract TAssetRecord Load(Stream stream, TOptions options, string? name = null);
}