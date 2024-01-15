namespace Nagule.Graphics.PostProcessing;

public partial class ACESToneMappingManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.aces_tone_mapping.glsl");
    public override string EntryPoint { get; } = "ACESToneMapping";
}

[NaAssetModule<RACESToneMapping>(typeof(EffectManagerBase<,>))]
internal partial class ACESToneMappingModule;