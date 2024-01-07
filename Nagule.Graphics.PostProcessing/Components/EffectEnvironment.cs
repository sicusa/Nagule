namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(EffectEnvironment))]
[NaAsset]
public record REffectEnvironment : RFeatureAssetBase
{
    public REffectPipeline Pipeline { get; init; } = REffectPipeline.Empty;
}