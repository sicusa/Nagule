namespace Nagule;

using Sia;

public partial record struct Keyboard()
{
    public readonly record struct OnKeyStateChanged(Key Key, ButtonState State) : IEvent
    {
        public bool IsKeyPressed(Key key) => Key == key && State.Pressed;
    }

    public long Frame { get; set; }
    public EnumDictionary<Key, ButtonState> KeyStates { get; } = new();

    public readonly bool IsKeyPressed(Key key)
        => KeyStates[key].Pressed;

    public readonly bool IsKeyDown(Key key)
    {
        ref var state = ref KeyStates[key];
        return state.Pressed && state.Frame == Frame;
    }

    public readonly bool IsKeyUp(Key key)
    {
        ref var state = ref KeyStates[key];
        return !state.Pressed && state.Frame == Frame;
    }
}