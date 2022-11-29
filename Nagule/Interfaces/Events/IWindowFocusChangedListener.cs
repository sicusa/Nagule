namespace Nagule;

public interface IWindowFocusChangedListener
{
    void OnWindowFocusChanged(IContext context, bool focused);
}