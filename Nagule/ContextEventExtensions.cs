namespace Nagule;

public static class ContextEventExtensions
{
    public static void SetWindowPosition(this IContext context, int x, int y)
    {
        ref var window = ref context.Acquire<Window>();
        window.X = x;
        window.Y = y;

        foreach (var listener in context.GetListeners<IWindowMoveListener>()) {
            listener.OnWindowMove(context, x, y);
        }
    }

    public static void SetWindowSize(this IContext context, int width, int height)
    {
        ref var window = ref context.Acquire<Window>();
        window.Width = width;
        window.Height = height;

        foreach (var listener in context.GetListeners<IWindowResizeListener>()) {
            listener.OnWindowResize(context, width, height);
        }
    }

    public static void SetWindowFocused(this IContext context, bool focused)
    {
        ref var window = ref context.Acquire<Window>();

        if (window.IsFocused == focused) { return; }
        window.IsFocused = focused;

        foreach (var listener in context.GetListeners<IWindowFocusChangedListener>()) {
            listener.OnWindowFocusChanged(context, focused);
        }
    }

    public static void SetWindowState(this IContext context, WindowState state)
    {
        ref var window = ref context.Acquire<Window>();

        if (window.State == state) { return; }
        window.State = state;

        foreach (var listener in context.GetListeners<IWindowStateChangedListener>()) {
            listener.OnWindowStateChanged(context, state);
        }
    }

    public static void SetMousePosition(this IContext context, float x, float y)
    {
        ref var mouse = ref context.Acquire<Mouse>();
        mouse.DeltaX = x - mouse.X;
        mouse.DeltaY = y - mouse.Y;
        mouse.X = x;
        mouse.Y = y;
        
        foreach (var listener in context.GetListeners<IMouseMoveListener>()) {
            listener.OnMouseMove(context, x, y);
        }
    }

    public static void SetMouseWheel(this IContext context, float offsetX, float offsetY)
    {
        ref var mouse = ref context.Acquire<Mouse>();
        mouse.WheelOffsetX = offsetX;
        mouse.WheelOffsetY = offsetY;

        foreach (var listener in context.GetListeners<IMouseWheelListener>()) {
            listener.OnMouseWheel(context, offsetX, offsetY);
        }
    }

    public static void SetMouseInWindow(this IContext context, bool inWindow)
    {
        ref var mouse = ref context.Acquire<Mouse>();

        if (mouse.InWindow == inWindow) { return; }
        mouse.InWindow = inWindow;

        if (inWindow) {
            foreach (var listener in context.GetListeners<IMouseEnterListener>()) {
                listener.OnMouseEnter(context);
            }
        }
        else {
            foreach (var listener in context.GetListeners<IMouseExitListener>()) {
                listener.OnMouseExit(context);
            }
        }
    }

    public static void SetMouseDown(this IContext context, MouseButton button, KeyModifiers modifiers)
    {
        ref var state = ref context.Acquire<Mouse>().Buttons[button];

        if (state.Pressed) {
            state.Down = false;
            state.Up = false;
            return;
        }

        state = MouseButtonState.DownState;

        foreach (var listener in context.GetListeners<IMouseDownListener>()) {
            listener.OnMouseDown(context, button, modifiers);
        }
    }

    public static void SetMouseUp(this IContext context, MouseButton button, KeyModifiers modifiers)
    {
        ref var mouse = ref context.Acquire<Mouse>();
        mouse.Buttons[button] = MouseButtonState.UpState;

        foreach (var listener in context.GetListeners<IMouseUpListener>()) {
            listener.OnMouseUp(context, button, modifiers);
        }
    }

    public static void SetKeyDown(this IContext context, Key key, KeyModifiers modifiers)
    {
        ref var keyboard = ref context.Acquire<Keyboard>();
        keyboard.Keys[key] = KeyState.DownState;
        keyboard.Modifiers = modifiers;

        foreach (var listener in context.GetListeners<IKeyDownListener>()) {
            listener.OnKeyDown(context, key, modifiers);
        }
    }

    public static void SetKeyUp(this IContext context, Key key, KeyModifiers modifiers)
    {
        ref var keyboard = ref context.Acquire<Keyboard>();
        keyboard.Keys[key] = KeyState.UpState;
        keyboard.Modifiers = modifiers;

        foreach (var listener in context.GetListeners<IKeyUpListener>()) {
            listener.OnKeyUp(context, key, modifiers);
        }
    }
}