namespace Nagule;

using System.Numerics;

using Sia;

public partial record struct Mouse()
{
    public readonly record struct OnButtonStateChanged(MouseButton Button, ButtonState State) : IEvent;
    public readonly record struct OnPositionChanged(Vector2 Value) : IEvent;
    public readonly record struct OnWheelOffsetChanged(Vector2 Value) : IEvent;
    public readonly record struct OnInWindowChanged(bool Value) : IEvent;

    [SiaProperty] public Vector2 Position { get; set; }
    public Vector2 Delta { get; set; }
    public Vector2 WheelOffset { get; set; }
    public bool InWindow { get; set; }

    public long Frame { get; set; }
    public EnumDictionary<MouseButton, ButtonState> ButtonStates { get; } = new();

    public readonly bool IsButtonPressed(MouseButton button)
        => ButtonStates[button].Pressed;

    public readonly bool IsButtonDown(MouseButton button)
    {
        ref var state = ref ButtonStates[button];
        return state.Pressed && state.Frame == Frame;
    }

    public readonly bool IsButtonUp(MouseButton button)
    {
        ref var state = ref ButtonStates[button];
        return !state.Pressed && state.Frame == Frame;
    }
}