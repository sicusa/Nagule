namespace Nagule.Graphics.Backend.OpenTK;

using System.Data;
using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderSettingsManager : GraphicsAssetManagerBase<RenderSettings, RenderSettingsAsset, RenderSettingsState>
{
    [AllowNull] private CubemapManager _cubemapManager;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _cubemapManager = world.GetAddon<CubemapManager>();

        Listen((EntityRef entity, ref RenderSettings snapshot, in RenderSettings.SetSkybox cmd) => {
            if (snapshot.Skybox != null) {
                entity.UnreferAsset(_cubemapManager.Get(snapshot.Skybox));
            }
            EntityRef? skyboxEntity = cmd.Value != null ? _cubemapManager.Acquire(cmd.Value, entity) : null;
            RenderFrame.Enqueue(entity, () => {
                RenderStates.Get(entity).SkyboxEntity = skyboxEntity;
                return true;
            });
        });

        Listen((EntityRef entity, in RenderSettings.SetAutoResizeByWindow cmd) => {
            if (cmd.Value) {
                var window = World.GetAddon<PrimaryWindow>().Entity;
                var (width, height) = window.Get<Window>().Size;
                RenderFrame.Enqueue(entity, () => {
                    ref var state = ref RenderStates.Get(entity);
                    state.Width = width;
                    state.Height = height;
                    return true;
                });
            }
        });

        Listen((EntityRef entity, in RenderSettings.SetSize cmd) => {
            var (width, height) = cmd.Value;
            RenderFrame.Enqueue(entity, () => {
                ref var state = ref RenderStates.Get(entity);
                state.Width = width;
                state.Height = height;
                return true;
            });
        });

        Listen((EntityRef entity, in RenderSettings.SetEffects cmd) => {

        });
    }

    protected override void LoadAsset(EntityRef entity, ref RenderSettings asset)
    {
        EntityRef? skyboxEntity = asset.Skybox != null ? _cubemapManager.Acquire(asset.Skybox, entity) : null;
        var (width, height) = asset.Size;

        if (asset.AutoResizeByWindow) {
            var window = World.GetAddon<PrimaryWindow>().Entity;
            (width, height) = window.Get<Window>().Size;
        }

        RenderFrame.Enqueue(entity, () => {
            var state = new RenderSettingsState {
                SkyboxEntity = skyboxEntity,
                Width = width,
                Height = height
            };
            RenderStates.Set(entity, state);
            return true;
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref RenderSettings asset)
    {
        if (asset.Skybox != null) {
            entity.UnreferAsset(_cubemapManager.Get(asset.Skybox));
        }
        RenderFrame.Enqueue(entity, () => {
            RenderStates.Remove(entity);
            return true;
        });
    }
}