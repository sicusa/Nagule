namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Sia;

public class AssetLibrary : ViewBase<TypeUnion<AssetMetadata>>
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

    [AllowNull] public ILogger Logger { get; private set; }

    public IReadOnlyDictionary<ObjectKey<IAssetRecord>, EntityRef> Entities => _entities;

    public EntityRef this[IAssetRecord record]
        => _entities.TryGetValue(new(record), out var entity)
            ? entity : throw new KeyNotFoundException("Asset entity not found");

    private readonly Dictionary<ObjectKey<IAssetRecord>, EntityRef> _entities = [];

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

    public EntityRef CreateEntity(
        IAssetRecord record, AssetLife life = AssetLife.Automatic)
    {
        var type = record.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset record type");
        }
        return entry.EntityCreator(World, record, life);
    }

    public EntityRef CreateEntity(
        IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = CreateEntity(record, life);
        referrer.Refer(entity);
        return entity;
    }

    public EntityRef CreateEntity<TComponentBundle>(
        IAssetRecord record, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)
        where TComponentBundle : struct, IComponentBundle
    {
        var type = record.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset record type");
        }
        return entry.EntityCreatorEx.Create(World, record, bundle, life);
    }

    public EntityRef AcquireEntity(
        IAssetRecord record, AssetLife life = AssetLife.Persistent)
    {
        var key = new ObjectKey<IAssetRecord>(record);
        if (!_entities.TryGetValue(key, out var entity)) {
            entity = CreateEntity(record, life);
            _entities.Add(key, entity);
        }
        return entity;
    }

    public EntityRef AcquireEntity(
        IAssetRecord record, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = AcquireEntity(record, life);
        referrer.Refer(entity);
        return entity;
    }

    public EntityRef AcquireEntity<TComponentBundle>(
        IAssetRecord record, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)
        where TComponentBundle : struct, IComponentBundle
    {
        var key = new ObjectKey<IAssetRecord>(record);
        if (!_entities.TryGetValue(key, out var entity)) {
            entity = CreateEntity(record, bundle, life);
            _entities.Add(key, entity);
        }
        return entity;
    }

    public bool TryGet(IAssetRecord record, out EntityRef entity)
        => _entities.TryGetValue(new(record), out entity);

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        Logger = world.CreateLogger<AssetLibrary>();
    }

    protected override void OnEntityAdded(in EntityRef entity) {}
    protected override void OnEntityRemoved(in EntityRef entity)
    {
        ref var metadata = ref entity.Get<AssetMetadata>();
        if (metadata.Referrers.Count != 0) {
            Logger.LogWarning("Destroyed asset [{Entity}] is referred by other assets.",
                entity.GetDisplayName());
            return;
        }
        var assetRecord = metadata.AssetRecord;
        if (assetRecord != null) {
            _entities.Remove(new(assetRecord));
        }
        DestroyAssetRecursively(entity, ref metadata);
    }

    private static void DestroyAssetRecursively(in EntityRef entity, ref AssetMetadata meta)
    {
        foreach (var referred in meta.Referred) {
            entity.Unrefer(referred);

            ref var refereeMeta = ref referred.Get<AssetMetadata>();
            if (refereeMeta.AssetLife == AssetLife.Automatic
                    && refereeMeta.Referrers.Count == 0) {
                DestroyAssetRecursively(referred, ref refereeMeta);
                referred.Dispose();
            }
        }
    }
}