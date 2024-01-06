namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Generator3D))]
[NaAsset<Generator3D>]
public record RGenerator3D(
    Func<World, EntityRef, IEnumerable<RNode3D>> Func) : RFeatureAssetBase
{
    public RGenerator3D(IEnumerable<RNode3D> enumerable)
        : this((world, entity) => enumerable) {}

    public RGenerator3D(Func<EntityRef, IEnumerable<RNode3D>> func)
        : this((world, entity) => func(entity)) {}
}