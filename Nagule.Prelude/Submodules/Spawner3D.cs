namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Spawner3D))]
[NaAsset]
public record RSpawner3D(
    Func<World, EntityRef, IEnumerable<RNode3D>> Func) : RFeatureBase
{
    public RSpawner3D(IEnumerable<RNode3D> enumerable)
        : this((world, entity) => enumerable) {}

    public RSpawner3D(Func<EntityRef, IEnumerable<RNode3D>> func)
        : this((world, entity) => func(entity)) {}
}

public partial class Spawner3DManager
{
    public override void LoadAsset(in EntityRef entity, ref Spawner3D asset, EntityRef stateEntity)
    {
        var node = entity.GetFeatureNode();
        foreach (var nodeRecord in asset.Func(World, node)) {
            Node3D.CreateEntity(World, nodeRecord, node);
        }
        entity.Dispose();
    }
}

[NaAssetModule<RSpawner3D>]
public partial class Spawner3DModule;