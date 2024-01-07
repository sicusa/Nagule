namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Generator3D))]
[NaAsset]
public record RGenerator3D(
    Func<World, EntityRef, IEnumerable<RNode3D>> Func) : RFeatureBase
{
    public RGenerator3D(IEnumerable<RNode3D> enumerable)
        : this((world, entity) => enumerable) {}

    public RGenerator3D(Func<EntityRef, IEnumerable<RNode3D>> func)
        : this((world, entity) => func(entity)) {}
}


public partial class Generator3DManager
{
    protected override void LoadAsset(EntityRef entity, ref Generator3D asset)
    {
        var node = entity.GetFeatureNode();
        foreach (var nodeRecord in asset.Func(World, node)) {
            Node3D.CreateEntity(World, nodeRecord, node);
        }
        entity.Dispose();
    }
}

[NaAssetModule<RGenerator3D>]
public partial class Generator3DModule;