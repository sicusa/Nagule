namespace Nagule.Graphics.UI;

using System.Numerics;
using ImGuiNET;
using Sia;

public class ImGuiEventDispatcher : ViewBase
{
    public int WindowWidth { get; internal set; }
    public int WindowHeight { get; internal set; }
    public Vector2 ScreenScale { get; internal set; }
    public Matrix4x4 WindowMatrix { get; internal set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        var layers = world.Query<TypeUnion<ImGuiContext>>();

        void UpdateImGui<TData>(in TData data, Action<TData, ImGuiIOPtr> action)
        {
            foreach (var layer in layers) {
                var ctx = layer.Get<ImGuiContext>().Pointer;
                ImGui.SetCurrentContext(ctx);
                ImGuiIOPtr io = ImGui.GetIO();
                action(data, io);
            }
        }

        Listen((in EntityRef entity, in Window.OnScreenScaleChanged cmd) => {
            ScreenScale = cmd.Value;
            UpdateImGui(this, (s, io) =>
                io.DisplaySize = new Vector2(s.WindowWidth, s.WindowHeight) / s.ScreenScale);
        });

        Listen((in EntityRef entity, in Window.OnSizeChanged cmd) => {
            (WindowWidth, WindowHeight) = cmd.Value;
            UpdateWindowMatrix();

            UpdateImGui(this, (s, io) =>
                io.DisplaySize = new Vector2(s.WindowWidth, s.WindowHeight) / s.ScreenScale);
        });

        Listen((in EntityRef entity, in Window.OnFocusChanged cmd) => {
            UpdateImGui(cmd.Value, (v, io) => io.AddFocusEvent(v));
        });

        Listen((in EntityRef entity, in Window.OnTextInput cmd) => {
            UpdateImGui(cmd.Character, (c, io) => io.AddInputCharacter(c));
        });

        Listen((in EntityRef entity, in Mouse.OnPositionChanged cmd) => {
            UpdateImGui(cmd.Value / ScreenScale, (p, io) => io.AddMousePosEvent(p.X, p.Y));
        });

        Listen((in EntityRef entity, in Mouse.OnWheelOffsetChanged cmd) => {
            UpdateImGui(cmd.Value, (o, io) => io.AddMouseWheelEvent(o.X, o.Y));
        });

        Listen((in EntityRef entity, in Mouse.OnButtonStateChanged cmd) => {
            UpdateImGui(cmd, (cmd, io) => io.AddMouseButtonEvent((int)cmd.Button, cmd.State.Pressed));
        });

        Listen((in EntityRef entity, in Keyboard.OnKeyStateChanged cmd) => {
            if (ImGuiUtils.TryMapKey(cmd.Key, out var imGuiKey)) {
                UpdateImGui((imGuiKey, cmd.State.Pressed), (d, io) => {
                    io.AddKeyEvent(d.imGuiKey, d.Pressed);
                });
            }
        });
    }

    internal void UpdateWindowMatrix()
    {
        WindowMatrix = Matrix4x4.CreateOrthographicOffCenter(
            0.0f, WindowWidth, WindowHeight, 0.0f, -1.0f, 1.0f);
    }
}