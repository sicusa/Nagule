namespace Nagule;

public interface IMousePressedListener
{
    void OnMousePressed(IContext context, MouseButton button, KeyModifiers modifiers);
}