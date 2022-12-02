namespace Nagule;

public interface IEngineUpdateListener
{
    void OnEngineUpdate(IContext context, float deltaTime);
}