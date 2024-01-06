namespace Nagule.Prelude;

using Sia;

public class Generator3DManager : AssetManager<Generator3D, RGenerator3D>
{
    protected override void LoadAsset(EntityRef entity, ref Generator3D asset)
    {
        var node = entity.GetFeatureNode();
        if (node.Valid) {
            foreach (var nodeRecord in asset.Func(World, node)) {
                Node3D.CreateEntity(World, nodeRecord, node);
            }
        }
        entity.Destroy();
    }
}

public class Generator3DModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Generator3DManager>(world);
    }
}