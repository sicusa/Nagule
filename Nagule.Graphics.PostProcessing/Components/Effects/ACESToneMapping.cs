namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(ACESToneMapping))]
[NaAsset<ACESToneMapping>]
public record RACESToneMapping : REffectBase
{
}