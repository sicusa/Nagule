namespace Nagule;

public interface IUpdateListener
{
    void OnUpdate(IContext context, float deltaTime);
}