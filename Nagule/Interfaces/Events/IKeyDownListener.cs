namespace Nagule;

public interface IKeyDownListener
{
    void OnKeyDown(IContext context, Key key, KeyModifiers modifiers);
}