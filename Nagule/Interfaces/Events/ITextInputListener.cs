namespace Nagule;

public interface ITextInputListener
{
    void OnTextInput(IContext context, char unicode);
}