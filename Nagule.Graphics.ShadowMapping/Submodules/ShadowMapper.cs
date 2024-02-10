namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(ShadowMapper))]
[NaAsset]
[NaRequireFeature<RCamera3D>]
public record RShadowMapper : RFeatureBase
{
}

public partial class ShadowMapperManager
{
    public override void LoadAsset(in EntityRef entity, ref ShadowMapper asset, EntityRef stateEntity)
    {
    }
}

[NaAssetModule<RShadowMapper>]
public partial class ShadowMapperModule;