namespace Nagule;

using Sia;

public abstract record AssetRefer<TAssetRecord>
    where TAssetRecord : class, IAssetRecord
{
    public sealed record Record(TAssetRecord Value) : AssetRefer<TAssetRecord>;
    public sealed record Id(Guid Value) : AssetRefer<TAssetRecord>;
    public sealed record Name(string Value) : AssetRefer<TAssetRecord>;

    public static implicit operator AssetRefer<TAssetRecord>(TAssetRecord record)
        => new Record(record);

    public static implicit operator AssetRefer<TAssetRecord>(Guid id)
        => new Id(id);

    public static implicit operator AssetRefer<TAssetRecord>(string name)
        => new Name(name);

    public EntityRef? Find(World world)
    {
        EntityRef entity;
        switch (this) {
            case Record(var record):
                var assetLib = world.GetAddon<AssetLibrary>();
                return assetLib.TryGet(record, out entity) ? entity : null;
            case Id(var id):
                var mapper = world.GetAddon<Mapper<Guid>>();
                return mapper.TryGetValue(id, out entity) ? entity : null; 
            case Name(var name):
                var aggr = world.GetAddon<Aggregator<Nagule.Name>>();
                return aggr.Find(name)?.First;
            default:
                return DoFind(world);
        }
    }

    protected virtual EntityRef? DoFind(World world) => null;
}