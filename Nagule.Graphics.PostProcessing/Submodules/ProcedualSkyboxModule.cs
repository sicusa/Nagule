namespace Nagule.Graphics.PostProcessing;

using System.Numerics;
using Sia;

public partial class ProcedualSkyboxManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.procedual_skybox.glsl");
    public override string EntryPoint { get; } = "ProcedualSkybox";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<ProcedualSkybox.SetLightSamples, int>((in ProcedualSkybox e) => Dyn.From(e.LightSamples));
        RegisterProperty<ProcedualSkybox.SetSamples, int>((in ProcedualSkybox e) => Dyn.From(e.Samples));
        RegisterProperty<ProcedualSkybox.SetGround, Vector3>((in ProcedualSkybox e) => Dyn.From(e.Ground));
        RegisterProperty<ProcedualSkybox.SetStarsLayers, int>((in ProcedualSkybox e) => Dyn.From(e.StarsLayers));
        RegisterProperty<ProcedualSkybox.SetStarsElevation, float>((in ProcedualSkybox e) => Dyn.From(e.StarsElevation));
        RegisterProperty<ProcedualSkybox.SetStarsAzimuth, float>((in ProcedualSkybox e) => Dyn.From(e.StarsAzimuth));
    }
}

[NaAssetModule<RProcedualSkybox>(typeof(EffectManagerBase<,>))]
internal partial class ProcedualSkyboxModule;