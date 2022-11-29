namespace Nagule;

public interface IWindowStateChangedListener
{
    void OnWindowStateChanged(IContext context, WindowState state);
}