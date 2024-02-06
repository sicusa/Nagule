namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(ShadowMapper))]
[NaAsset]
[NaRequireFeature<RCamera3D>]
public record RShadowMapper : RFeatureBase
{
}