namespace Nagule;

using Aeco;

public struct KeyState
{
    public static readonly KeyState DownState = new KeyState {
        Down = true,
        Pressed = true,
        Up = false
    };
    public static readonly KeyState PressedState = new KeyState {
        Down = false,
        Pressed = true,
        Up = false
    };
    public static readonly KeyState UpState = new KeyState {
        Down = false,
        Pressed = false,
        Up = true
    };
    public static readonly KeyState EmptyState = new KeyState {
        Down = false,
        Pressed = false,
        Up = false
    };

    public bool Pressed;
    public bool Down;
    public bool Up;
}

public struct Keyboard : ISingletonComponent
{
    public readonly EnumArray<Key, KeyState> States = new();
    public KeyModifiers Modifiers = 0;

    public Keyboard() {}
}