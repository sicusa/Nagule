namespace Nagule;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class UnusedAssetDestroySystem()
    : SystemBase(
        matcher: Matchers.Of<AssetMetadata>(),
        trigger: EventUnion.Of<WorldEvents.Add, AssetMetadata.OnUnreferred>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(world, (world, entity) => {
            ref var metadata = ref entity.Get<AssetMetadata>();
            if (metadata.AssetLife == AssetLife.Automatic
                    && metadata.Referrers.Count == 0) {
                entity.Dispose();
            }
        });
    }
}

public class AssetSystemModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<UnusedAssetDestroySystem>())
{
    private interface IEntityCreatorEx
    {
        EntityRef Create<TComponentBundle>(
            World world, IAssetRecord record, in TComponentBundle bundle, AssetLife life)
            where TComponentBundle : struct, IComponentBundle;
    }

    private class EntityCreatorEx<TAsset, TAssetRecord> : IEntityCreatorEx
        where TAsset : struct, IAsset<TAssetRecord>, IConstructable<TAsset, TAssetRecord>
        where TAssetRecord : class, IAssetRecord
    {
        public EntityRef Create<TComponentBundle>(
            World world, IAssetRecord record, in TComponentBundle bundle, AssetLife life)
            where TComponentBundle : struct, IComponentBundle
            => TAsset.CreateEntity(world, Unsafe.As<TAssetRecord>(record), bundle, life);
    }

    private record struct AssetEntry(
        Func<World, IAssetRecord, AssetLife, EntityRef> EntityCreator,
        IEntityCreatorEx EntityCreatorEx);

    private static readonly Dictionary<Type, AssetEntry> s_assetEntries = [];

    public static void RegisterAsset<TAsset, TAssetRecord>()
        where TAsset : struct, IAsset<TAssetRecord>, IConstructable<TAsset, TAssetRecord>
        where TAssetRecord : class, IAssetRecord
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
            s_assetEntries, typeof(TAssetRecord), out bool exists);
        if (exists) { return; }

        entry.EntityCreator = (world, record, life) =>
            TAsset.CreateEntity(world, Unsafe.As<TAssetRecord>(record), life);
        entry.EntityCreatorEx = new EntityCreatorEx<TAsset, TAssetRecord>();
    }

    public static EntityRef UnsafeCreateEntity(World world, IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = UnsafeCreateEntity(world, record, life);
        referrer.Refer(entity);
        return entity;
    }

    public static EntityRef UnsafeCreateEntity<TComponentBundle>(
        World world, IAssetRecord record, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)
        where TComponentBundle : struct, IComponentBundle
    {
        var type = record.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset record type");
        }
        return entry.EntityCreatorEx.Create(world, record, bundle, life);
    }

    public static EntityRef UnsafeCreateEntity(World world, IAssetRecord record, AssetLife life = AssetLife.Automatic)
    {
        var type = record.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset record type");
        }
        return entry.EntityCreator(world, record, life);
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<AssetLibrary>(world);
    }
}