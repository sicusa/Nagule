namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public partial class RenderSettingsManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in RenderSettings.SetPipelineProvider cmd) => {
            ref var settings = ref entity.Get<RenderSettings>();
            settings.PipelineProvider = cmd.Value;
            RecreateRenderPassChain(entity, settings);
        });

        Listen((in EntityRef entity, in RenderSettings.SetIsOcclusionCullingEnabled cmd) => {
            ref var settings = ref entity.Get<RenderSettings>();
            settings.IsOcclusionCullingEnabled = cmd.Value;
            RecreateRenderPassChain(entity, settings);
        });

        Listen((in EntityRef entity, in RenderSettings.SetResolution cmd) => {
            var resolution = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state.Resolution = resolution;
            });
        });

        Listen((EntityRef entity, in RenderSettings.SetSunLight cmd) => {
            var sunLightState = cmd.Value?.Find(World)?.GetStateEntity();
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state.SunLightState = sunLightState;
            });
        });
    }

    public override void LoadAsset(in EntityRef entity, ref RenderSettings asset, EntityRef stateEntity)
    {
        var resolution = asset.Resolution;
        var sunLightState = asset.SunLight?.Find(World)?.GetStateEntity();

        RecreateRenderPassChain(entity, asset);

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<RenderSettingsState>();
            state = new RenderSettingsState {
                Resolution = resolution,
                SunLightState = sunLightState
            };
        });
    }

    public override void UnloadAsset(in EntityRef entity, in RenderSettings asset, EntityRef stateEntity) {}

    private void RecreateRenderPassChain(
        EntityRef settingsEntity, in RenderSettings settings)
    {
        var provider = settings.PipelineProvider ?? StandardPipelineProvider.Instance;

        settingsEntity.GetState<RenderSettingsState>().RenderPassChain =
            provider.TransformPipeline(RenderPassChain.Empty, settings);

        foreach (var cameraEntity in settingsEntity.FindReferrers<Camera3D>()) {
            World.Send(cameraEntity, Camera3D.OnRenderPipelineDirty.Instance);
        }
    }
}