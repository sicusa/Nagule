namespace Nagule;

using Sia;

public abstract class AssetModuleBase : AddonSystemBase
{
    public AssetModuleBase() {}

    public AssetModuleBase(
        SystemChain? children = null, IEntityMatcher? matcher = null,
        IEventUnion? trigger = null, IEventUnion? filter = null)
    {
        Children = children;
        Matcher = matcher;
        Trigger = trigger;
        Filter = filter;
    } 

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        RegisterAssetManager(world);
    }

    protected abstract void RegisterAssetManager(World world);
}

public abstract class AssetModuleBase<TAssetManager> : AssetModuleBase
    where TAssetManager : IAddon, new()
{
    protected sealed override void RegisterAssetManager(World world)
        => AddAddon<TAssetManager>(world);
}