namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class FastApproximateAntiAliasingManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.fxaa.glsl");
    public override string EntryPoint { get; } = "FXAA";

}

[NaAssetModule<RFastApproximateAntiAliasing>(typeof(EffectManagerBase<>))]
internal partial class FastApproximateAntiAliasingModule;