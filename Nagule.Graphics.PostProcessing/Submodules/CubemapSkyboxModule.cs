namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class CubemapSkyboxManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.cubemap_skybox.glsl");
    public override string EntryPoint { get; } = "CubemapSkybox";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<CubemapSkybox.SetCubemap, RCubemap>((in CubemapSkybox e) => new TextureDyn(e.Cubemap));
        RegisterProperty<CubemapSkybox.SetExposure, float>((in CubemapSkybox e) => Dyn.From(e.Exposure));
    }
}

[NaAssetModule<RCubemapSkybox>(typeof(EffectManagerBase<,>))]
internal partial class CubemapSkyboxModule;