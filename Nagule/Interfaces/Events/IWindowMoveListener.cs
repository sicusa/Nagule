namespace Nagule;

public interface IWindowMoveListener
{
    void OnWindowMove(IContext context, int x, int y);
}