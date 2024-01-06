namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Updator))]
[NaAsset<Updator>]
public record RUpdator(Action<World, EntityRef, SimulationFrame> Action) : RFeatureAssetBase
{
    public RUpdator(Action<EntityRef, SimulationFrame> action)
        : this((world, entity, frame) => action(entity, frame)) {}
}