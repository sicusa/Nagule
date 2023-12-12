namespace Nagule;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class UnusedAssetDestroySystem : SystemBase
{
    public UnusedAssetDestroySystem()
    {
        Matcher = Matchers.Of<AssetMetadata>();
        Trigger = EventUnion.Of<WorldEvents.Add, AssetMetadata.OnUnreferred>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(world, (world, entity) => {
            ref var metadata = ref entity.Get<AssetMetadata>();
            if (metadata.AssetLife == AssetLife.Automatic
                    && metadata.Referrers.Count == 0) {
                world.Destroy(entity);
            }
        });
    }
}

public class AssetModule : AddonSystemBase
{
    private interface IEntityCreatorEx
    {
        EntityRef Create<TComponentBundle>(
            World world, IAsset template, in TComponentBundle bundle, AssetLife life)
            where TComponentBundle : struct, IComponentBundle;
    }

    private class EntityCreatorEx<TAsset, TAssetTemplate> : IEntityCreatorEx
        where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
        where TAssetTemplate : class, IAsset
    {
        public EntityRef Create<TComponentBundle>(
            World world, IAsset template, in TComponentBundle bundle, AssetLife life)
            where TComponentBundle : struct, IComponentBundle
            => TAsset.CreateEntity(world, Unsafe.As<TAssetTemplate>(template), bundle, life);
    }

    private record struct AssetEntry(
        Func<World, IAsset, AssetLife, EntityRef> EntityCreator,
        IEntityCreatorEx EntityCreatorEx);

    private static readonly Dictionary<Type, AssetEntry> s_assetEntries = [];

    public static void RegisterAsset<TAsset, TAssetTemplate>()
        where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
        where TAssetTemplate : class, IAsset
    {
        ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
            s_assetEntries, typeof(TAssetTemplate), out bool exists);
        if (exists) { return; }

        entry.EntityCreator = (world, template, life) =>
            TAsset.CreateEntity(world, Unsafe.As<TAssetTemplate>(template), life);
        entry.EntityCreatorEx = new EntityCreatorEx<TAsset, TAssetTemplate>();
    }

    public static EntityRef UnsafeCreateEntity(World world, IAsset template, EntityRef referrer, AssetLife life = AssetLife.Automatic)
    {
        var entity = UnsafeCreateEntity(world, template, life);
        referrer.Modify(new AssetMetadata.Refer(entity));
        return entity;
    }

    public static EntityRef UnsafeCreateEntity<TComponentBundle>(
        World world, IAsset template, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)
        where TComponentBundle : struct, IComponentBundle
    {
        var type = template.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset template type");
        }
        return entry.EntityCreatorEx.Create(world, template, bundle, life);
    }

    public static EntityRef UnsafeCreateEntity(World world, IAsset template, AssetLife life = AssetLife.Automatic)
    {
        var type = template.GetType();
        if (!s_assetEntries.TryGetValue(type, out var entry)) {
            throw new ArgumentException("Unregistered asset template type");
        }
        return entry.EntityCreator(world, template, life);
    }

    public AssetModule()
    {
        Children = SystemChain.Empty
            .Add<UnusedAssetDestroySystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<AssetLibrary>(world);
    }
}