namespace Nagule.Graphics.PostProcessing;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(EffectPipeline))]
[NaAsset<EffectPipeline>]
public record REffectPipeline : AssetBase
{
    public static readonly REffectPipeline Empty = new();
    public static readonly REffectPipeline Default = new() {
        Effects = [
            new RACESToneMapping(),
            new RGammaCorrection()
        ]
    };

    [SiaProperty(Item = "Effect")]
    public ImmutableList<REffectBase> Effects { get; init; } = [];
}