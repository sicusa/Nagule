namespace Nagule;

public interface IKeyUpListener
{
    void OnKeyUp(IContext context, Key key, KeyModifiers modifiers);
}