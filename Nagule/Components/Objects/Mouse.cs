namespace Nagule;

using Aeco;

public struct MouseButtonState
{
    public static readonly MouseButtonState DownState = new MouseButtonState {
        Down = true,
        Pressed = true,
        Up = false
    };
    public static readonly MouseButtonState PressedState = new MouseButtonState {
        Down = false,
        Pressed = true,
        Up = false
    };
    public static readonly MouseButtonState UpState = new MouseButtonState {
        Down = false,
        Pressed = false,
        Up = true
    };
    public static readonly MouseButtonState EmptyState = new MouseButtonState {
        Down = false,
        Pressed = false,
        Up = false
    };

    public bool Pressed;
    public bool Down;
    public bool Up;
}

public struct Mouse : IReactiveSingletonComponent
{
    public float X = 0;
    public float Y = 0;
    public float DeltaX = 0;
    public float DeltaY = 0;
    public float WheelOffsetX = 0;
    public float WheelOffsetY = 0;
    public bool InWindow = true;

    public readonly EnumArray<MouseButton, MouseButtonState> Buttons = new();

    public Mouse() {}
}