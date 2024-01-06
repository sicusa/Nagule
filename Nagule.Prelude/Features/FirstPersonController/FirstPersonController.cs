namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(FirstPersonController))]
[NaAsset<FirstPersonController>]
public record RFirstPersonController : RFeatureAssetBase
{
    public float Rate { get; init; } = 10;
    public float Sensitivity { get; init; } = 0.005f;
}