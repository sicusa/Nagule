namespace Nagule;

public interface IRenderFinishedListener
{
    void OnRenderFinished(IContext context, float deltaTime);
}