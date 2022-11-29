namespace Nagule;

public interface IMouseDownListener
{
    void OnMouseDown(IContext context, MouseButton button, KeyModifiers modifiers);
}