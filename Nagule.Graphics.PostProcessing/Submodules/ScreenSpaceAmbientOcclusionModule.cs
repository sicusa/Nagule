namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class ScreenSpaceAmbientOcclusionManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.ssao.glsl");
    public override string EntryPoint { get; } = "SSAO";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<ScreenSpaceAmbientOcclusion.SetSamples, int>((in ScreenSpaceAmbientOcclusion e) => Dyn.From(e.Samples));
        RegisterProperty<ScreenSpaceAmbientOcclusion.SetRadius, float>((in ScreenSpaceAmbientOcclusion e) => Dyn.From(e.Radius));
    }
}

[NaAssetModule<RScreenSpaceAmbientOcclusion>(typeof(EffectManagerBase<,>))]
internal partial class ScreenSpaceAmbientOcclusionModule;