namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class UnusedResourceDestroyer : Layer, IFrameStartListener
{
    private Group<ResourceReferencers> _g = new();

    public void OnFrameStart(IContext context)
    {
        foreach (var id in _g.Query(context)) {
            if (context.Inspect<ResourceReferencers>(id).Ids.Count == 0) {
                context.Acquire<Destroy>(id);
            }
        }
    }
}