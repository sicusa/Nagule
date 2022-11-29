namespace Nagule;

public interface IMouseUpListener
{
    void OnMouseUp(IContext context, MouseButton button, KeyModifiers modifiers);
}