namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Events))]
[NaAsset<Events>]
public record REvents : RFeatureAssetBase
{
    public Action<World, EntityRef>? Start { get; init; }
    public Action<World, EntityRef>? Destroy { get; init; }
    public IEventListener? Listener { get; init; }
}