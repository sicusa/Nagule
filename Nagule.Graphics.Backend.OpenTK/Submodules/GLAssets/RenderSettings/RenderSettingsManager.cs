namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public partial class RenderSettingsManager
{
    [AllowNull] private CubemapManager _cubemapManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _cubemapManager = world.GetAddon<CubemapManager>();

        Listen((in EntityRef entity, ref RenderSettings snapshot, in RenderSettings.SetSkybox cmd) => {
            if (snapshot.Skybox != null) {
                entity.UnreferAsset(_cubemapManager[snapshot.Skybox]);
            }

            EntityRef? skyboxEntity = cmd.Value != null ? _cubemapManager.Acquire(cmd.Value, entity) : null;
            var skyboxStateEntity = skyboxEntity?.GetStateEntity();
            var stateEntity = entity.GetStateEntity();

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state.SkyboxState = skyboxStateEntity;
                return true;
            });
        });

        Listen((in EntityRef entity, in RenderSettings.SetAutoResizeByWindow cmd) => {
            if (!cmd.Value) {
                return;
            }

            var window = World.GetAddon<PrimaryWindow>().Entity;
            var (width, height) = window.Get<Window>().Size;
            var stateEntity = entity.GetStateEntity();

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state.Width = width;
                state.Height = height;
                return true;
            });
        });

        Listen((in EntityRef entity, in RenderSettings.SetSize cmd) => {
            var (width, height) = cmd.Value;
            var stateEntity = entity.GetStateEntity();

            RenderFrame.Enqueue(entity, () => {
                ref var state = ref stateEntity.Get<RenderSettingsState>();
                state.Width = width;
                state.Height = height;
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref RenderSettings asset, EntityRef stateEntity)
    {
        var (width, height) = asset.Size;

        if (asset.AutoResizeByWindow) {
            var window = World.GetAddon<PrimaryWindow>().Entity;
            (width, height) = window.Get<Window>().Size;
        }

        EntityRef? skyboxEntity = asset.Skybox != null ? _cubemapManager.Acquire(asset.Skybox, entity) : null;
        var skyboxStateEntity = skyboxEntity?.GetStateEntity();

        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<RenderSettingsState>();
            state = new RenderSettingsState {
                SkyboxState = skyboxStateEntity,
                Width = width,
                Height = height
            };
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref RenderSettings asset, EntityRef stateEntity)
    {
    }
}