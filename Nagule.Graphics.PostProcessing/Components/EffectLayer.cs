namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(EffectLayer))]
[NaAsset]
[NaRequireFeature<RCamera3D>]
public record REffectLayer : RFeatureBase
{
    [Sia(NoCommands = true)]
    public required REffectPipeline Pipeline { get; init; }
}