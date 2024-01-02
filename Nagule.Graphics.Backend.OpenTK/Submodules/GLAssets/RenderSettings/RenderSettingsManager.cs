namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderSettingsManager
    : GraphicsAssetManagerBase<RenderSettings, RRenderSettings, RenderSettingsState>
{
    [AllowNull] private CubemapManager _cubemapManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _cubemapManager = world.GetAddon<CubemapManager>();

        Listen((EntityRef entity, ref RenderSettings snapshot, in RenderSettings.SetSkybox cmd) => {
            if (snapshot.Skybox != null) {
                entity.UnreferAsset(_cubemapManager[snapshot.Skybox]);
            }
            EntityRef? skyboxEntity = cmd.Value != null ? _cubemapManager.Acquire(cmd.Value, entity) : null;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<RenderSettingsState>();
                state.SkyboxEntity = skyboxEntity;
                return true;
            });
        });

        Listen((EntityRef entity, in RenderSettings.SetAutoResizeByWindow cmd) => {
            if (cmd.Value) {
                var window = World.GetAddon<PrimaryWindow>().Entity;
                var (width, height) = window.Get<Window>().Size;
                RenderFrame.Enqueue(entity, () => {
                    ref var state = ref entity.GetState<RenderSettingsState>();
                    state.Width = width;
                    state.Height = height;
                    return true;
                });
            }
        });

        Listen((EntityRef entity, in RenderSettings.SetSize cmd) => {
            var (width, height) = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref entity.GetState<RenderSettingsState>();
                state.Width = width;
                state.Height = height;
                return true;
            });
        });
    }

    protected override void LoadAsset(EntityRef entity, ref RenderSettings asset, EntityRef stateEntity)
    {
        EntityRef? skyboxEntity = asset.Skybox != null ? _cubemapManager.Acquire(asset.Skybox, entity) : null;
        var (width, height) = asset.Size;

        if (asset.AutoResizeByWindow) {
            var window = World.GetAddon<PrimaryWindow>().Entity;
            (width, height) = window.Get<Window>().Size;
        }

        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<RenderSettingsState>();
            state = new RenderSettingsState {
                SkyboxEntity = skyboxEntity,
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