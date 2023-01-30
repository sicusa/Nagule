namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class UnusedResourceDestroyer : Layer, IFrameStartListener
{
    private Group<ResourceImplicit, Modified<ResourceReferencers>, ResourceReferencers> _g = new();

    public void OnFrameStart(IContext context)
    {
        foreach (var id in _g.Query(context)) {
            ref readonly var referencers = ref context.Inspect<ResourceReferencers>(id);
            if (referencers.Ids.Count == 0) {
                context.Acquire<Destroy>(id);
            }
        }
    }
}