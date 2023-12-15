namespace Nagule;

using Sia;

public struct PeripheralBundle : IComponentBundle
{
    public Window Window = new();
    public Cursor Cursor = new();
    public Keyboard Keyboard = new();
    public Mouse Mouse = new();

    public PeripheralBundle() {}
}