namespace Nagule;

using Sia;

public class UnusedAssetDestroySystem()
    : SystemBase(
        matcher: Matchers.Of<AssetMetadata>(),
        trigger: EventUnion.Of<WorldEvents.Add, AssetMetadata.OnUnreferred>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            if (!entity.Valid) { continue; }
            ref var metadata = ref entity.Get<AssetMetadata>();
            if (metadata.AssetLife == AssetLife.Automatic
                    && metadata.Referrers.Count == 0) {
                entity.Dispose();
            }
        }
    }
}

public class AssetSystemModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<UnusedAssetDestroySystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<AssetLibrary>(world);
    }
}