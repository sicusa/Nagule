namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class UnusedResourceDestroyer : VirtualLayer, ILateUpdateListener
{
    private Group<ResourceReferencers> _g = new();

    public void OnLateUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _g.Query(context)) {
            if (context.Inspect<ResourceReferencers>(id).Ids.Count == 0) {
                context.Acquire<Destroy>(id);
            }
        }
    }
}