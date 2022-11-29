namespace Nagule;

public struct Window : ISingletonComponent
{
    public WindowState State = WindowState.Normal;
    public bool IsFocused = true;

    public int X = 0;
    public int Y = 0;

    public int Width = 0;
    public int Height = 0;

    public Window() {}
}