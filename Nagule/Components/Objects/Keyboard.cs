namespace Nagule;

using Aeco;

public struct KeyState
{
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