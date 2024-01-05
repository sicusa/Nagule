namespace Nagule;

using System.Collections.Immutable;
using System.Numerics;
using Sia;

public partial record struct Window(
    [SiaProperty] (int, int) Size,
    [SiaProperty] string Title = "Nagule",
    [SiaProperty] WindowState State = WindowState.Normal,
    [SiaProperty] bool IsResizable = false,
    [SiaProperty] bool IsFullscreen = false,
    [SiaProperty] bool HasBorder = true,
    [SiaProperty] bool IsFocused = true,
    [SiaProperty] (int, int)? MaximumSize = null,
    [SiaProperty] (int, int)? MinimumSize = null,
    [SiaProperty] (int, int)? Location = null)
{
    public (int, int) ScreenSize { get; set; }
    public Vector2 ScreenScale { get; set; }
    public (int, int) PhysicalSize { get; set; }

    public class OnInitialized : SingletonEvent<OnInitialized> {}
    public class OnUninitialized : SingletonEvent<OnUninitialized> {}
    public class OnRefresh : SingletonEvent<OnRefresh> {}

    public readonly record struct OnSizeChanged((int, int) Value) : IEvent;
    public readonly record struct OnScreenSizeChanged((int, int) Value) : IEvent;
    public readonly record struct OnScreenScaleChanged(Vector2 Value) : IEvent;
    public readonly record struct OnStateChanged(WindowState Value) : IEvent;
    public readonly record struct OnIsFullscreenChanged(bool Value) : IEvent;
    public readonly record struct OnFocusChanged(bool Value) : IEvent;
    public readonly record struct OnLocationChanged((int, int) Value) : IEvent;
    public readonly record struct OnTextInput(char Character) : IEvent;
    public readonly record struct OnFileDrop(ImmutableArray<string> Files) : IEvent;
    public readonly record struct OnJoystickConnectionChanged(int Id, bool Connected) : IEvent;

    public Window() : this((1024, 720)) {}
}