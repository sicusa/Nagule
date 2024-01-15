namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public partial class RenderSettingsManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

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

        RenderFrame.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<RenderSettingsState>();
            state = new RenderSettingsState {
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