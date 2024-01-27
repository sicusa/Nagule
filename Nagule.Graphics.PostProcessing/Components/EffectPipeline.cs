namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(EffectPipeline))]
[NaAsset]
public record REffectPipeline(
    [property: SiaProperty(Item = "Effect")] ImmutableList<REffectBase> Effects)
    : AssetBase;