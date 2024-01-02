namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(EffectEnvironment))]
[NaAsset<EffectEnvironment>]
public record REffectEnvironment : FeatureAssetBase
{
    public REffectPipeline Pipeline { get; init; } = REffectPipeline.Empty;
}