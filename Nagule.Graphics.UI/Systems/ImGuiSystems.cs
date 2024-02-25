namespace Nagule.Graphics.UI;

using System.Text;
using ImGuiNET;
using Sia;

public class ImGuiUpdateSystem()
    : SystemBase(matcher: Matchers.Of<ImGuiContext>())
{
    private static readonly EnumDictionary<ImGuiMouseCursor, CursorStyle> s_cursorStyleMap = new() {
        [ImGuiMouseCursor.Arrow] = CursorStyle.Default,
        [ImGuiMouseCursor.TextInput] = CursorStyle.TextInput,
        [ImGuiMouseCursor.Hand] = CursorStyle.Hand,
        [ImGuiMouseCursor.ResizeAll] = CursorStyle.Default,
        [ImGuiMouseCursor.ResizeNS] = CursorStyle.ResizeVertical,
        [ImGuiMouseCursor.ResizeEW] = CursorStyle.ResizeHorizontal,
        [ImGuiMouseCursor.ResizeNESW] = CursorStyle.Default,
        [ImGuiMouseCursor.ResizeNWSE] = CursorStyle.Default,
        [ImGuiMouseCursor.NotAllowed] = CursorStyle.Default
    };

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        world.GetAddon<SimulationFramer>().Start(() => {
            var dispatcher = world.GetAddon<ImGuiEventDispatcher>();
            ref var window = ref world.GetAddon<PrimaryWindow>().Entity.Get<Window>();

            (dispatcher.WindowWidth, dispatcher.WindowHeight) = window.Size;
            dispatcher.ScreenScale = window.ScreenScale;
            dispatcher.UpdateWindowMatrix();
            return true;
        });
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var window = world.GetAddon<PrimaryWindow>().Entity;
        var screenScale = window.Get<Window>().ScreenScale;
        ref var cursor = ref window.Get<Cursor>();
        var simFrame = world.GetAddon<SimulationFramer>();
        var keyStates = window.Get<Keyboard>().KeyStates;

        foreach (var entity in query) {
            var context = entity.Get<ImGuiContext>().Pointer;

            ImGui.SetCurrentContext(context);
            var io = ImGui.GetIO();

            io.DisplayFramebufferScale = screenScale;
            io.DeltaTime = simFrame.DeltaTime;

            if ((io.ConfigFlags & ImGuiConfigFlags.NoMouseCursorChange) == 0) {
                UpdateCursor(io, window, cursor.State, cursor.Style);
            }
            UpdateImGuiEvents(io, keyStates);

            ImGui.NewFrame();
        }
    }

    private static void UpdateCursor(ImGuiIOPtr io, EntityRef window, CursorState state, CursorStyle style)
    {
        var imGuiCursor = ImGui.GetMouseCursor();
        var cursor = new Cursor.View(window);

        if (io.MouseDrawCursor || imGuiCursor == ImGuiMouseCursor.None) {
            if (state != CursorState.Hidden) {
                cursor.State = CursorState.Hidden;
            }
        }
        else {
            var desiredStyle = s_cursorStyleMap[imGuiCursor];
            if (style != desiredStyle) {
                cursor.Style = desiredStyle;
            }
            if (state == CursorState.Hidden) {
                cursor.State = CursorState.Normal;
            }
        }
    }

    private static void UpdateImGuiEvents(ImGuiIOPtr io, EnumDictionary<Key, ButtonState> ks)
    {
        io.AddKeyEvent(ImGuiKey.ModShift, ks[Key.LeftShift].Pressed || ks[Key.RightShift].Pressed);
        io.AddKeyEvent(ImGuiKey.ModCtrl, ks[Key.LeftControl].Pressed || ks[Key.RightControl].Pressed);
        io.AddKeyEvent(ImGuiKey.ModAlt, ks[Key.LeftAlt].Pressed || ks[Key.RightAlt].Pressed);
        io.AddKeyEvent(ImGuiKey.ModSuper, ks[Key.LeftSuper].Pressed || ks[Key.RightSuper].Pressed);
    }
}

public class ImGuiSystems()
    : SystemBase(
        children: SystemChain.Empty
            .Add<ImGuiUpdateSystem>());