namespace Nagule.Graphics.Backends.OpenTK;

using Microsoft.Extensions.Logging;
using Sia;

public partial class RenderSettingsManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in RenderSettings.SetResolution cmd) => {
            var resolution = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state.Resolution = resolution;
            });
        });

        Listen((in EntityRef entity, in RenderSettings.SetSunLight cmd) => {
            var sunLightRefer = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            SimulationFramer.Start(() => {
                var sunLightState = FilterSunLightStateEntity(sunLightRefer?.Find(World))?.GetStateEntity();

                RenderFramer.Enqueue(stateEntity, () => {
                    ref var state = ref stateEntity.Get<RenderSettingsState>();
                    state.SunLightState = sunLightState;
                });
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref RenderSettings asset, EntityRef stateEntity)
    {
        var resolution = asset.Resolution;
        var sunLightRefer = asset.SunLight;

        SimulationFramer.Start(() => {
            var sunLightState = FilterSunLightStateEntity(sunLightRefer?.Find(World))?.GetStateEntity();

            RenderFramer.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state = new RenderSettingsState {
                    Resolution = resolution,
                    SunLightState = sunLightState
                };
            });
        });
    }

    private EntityRef? FilterSunLightStateEntity(in EntityRef? entity)
    {
        if (entity != null && !entity.Value.Contains<Light3D>()) {
            Logger.LogError("Invalid reference for sun light entity: {Refer}", entity);
            return null;
        }
        return entity;
    }

    protected override void UnloadAsset(EntityRef entity, ref RenderSettings asset, EntityRef stateEntity)
    {
    }
}