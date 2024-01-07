namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(EffectEnvironment))]
[NaAsset]
public record REffectEnvironment : RFeatureBase
{
    public REffectPipeline Pipeline { get; init; } = REffectPipeline.Empty;
}