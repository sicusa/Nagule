namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class BloomManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.bloom.glsl");
    public override string EntryPoint { get; } = "Bloom";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<Bloom.SetThreshold, float>((in Bloom e) => Dyn.From(e.Threshold));
        RegisterProperty<Bloom.SetIntensity, float>((in Bloom e) => Dyn.From(e.Intensity));
        RegisterProperty<Bloom.SetRadius, float>((in Bloom e) => Dyn.From(e.Radius));
        RegisterProperty<Bloom.SetDirtTexture, RTexture2D?>((in Bloom e) => new TextureDyn(e.DirtTexture));
        RegisterProperty<Bloom.SetDirtIntensity, float>((in Bloom e) => Dyn.From(e.DirtIntensity));
    }
}

[NaAssetModule<RBloom>(typeof(EffectManagerBase<,>))]
internal partial class BloomModule;