namespace Nagule;

public interface ILateUpdateListener
{
    void OnLateUpdate(IContext context, float deltaTime);
}