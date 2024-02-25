namespace Nagule;

using System.Collections.Immutable;
using System.Numerics;
using Sia;

public partial record struct Window(
    [Sia] (int, int) Size,
    [Sia] string Title = "Nagule",
    [Sia] WindowState State = WindowState.Normal,
    [Sia] float RenderScale = 1f,
    [Sia] bool IsResizable = true,
    [Sia] bool IsFullscreen = false,
    [Sia] bool HasBorder = true,
    [Sia] bool IsFocused = true,
    [Sia] (int, int)? MaximumSize = null,
    [Sia] (int, int)? MinimumSize = null,
    [Sia] (int, int)? Location = null)
{
    public (int, int) ScreenSize { get; set; }
    public Vector2 ScreenScale { get; set; }

    public readonly (int, int) PhysicalScreenSize
        => ((int)(ScreenSize.Item1 * ScreenScale.X), (int)(ScreenSize.Item2 * ScreenScale.Y));

    public readonly (int, int) PhysicalSize
        => ((int)(Size.Item1 * ScreenScale.X), (int)(Size.Item2 * ScreenScale.Y));

    public readonly (int, int) ScaledSize
        => ((int)(Size.Item1 * RenderScale), (int)(Size.Item2 * RenderScale));

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