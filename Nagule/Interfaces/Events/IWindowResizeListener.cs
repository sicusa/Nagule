namespace Nagule;

public interface IWindowResizeListener
{
    void OnWindowResize(IContext context, int width, int height);
}