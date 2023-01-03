namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class DestroyedObjectCleaner : VirtualLayer, ILateUpdateListener
{
    private Group<Destroy> _g = new();

    public void OnLateUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _g.Query(context)) {
            context.Clear(id);
        }
    }
}