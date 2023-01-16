namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class UnusedResourceDestroyer : Layer, IUpdateListener
{
    private Group<ResourceReferencers> _g = new();

    public void OnUpdate(IContext context)
    {
        foreach (var id in _g.Query(context)) {
            if (context.Inspect<ResourceReferencers>(id).Ids.Count == 0) {
                Console.WriteLine(id);
                context.Acquire<Destroy>(id);
            }
        }
    }
}