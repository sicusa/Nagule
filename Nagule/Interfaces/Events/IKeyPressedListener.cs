namespace Nagule;

public interface IKeyPressedListener
{
    void OnKeyPressed(IContext context, Key key, KeyModifiers modifiers);
}