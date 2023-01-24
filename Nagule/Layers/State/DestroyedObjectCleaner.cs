namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class DestroyedObjectCleaner : Layer, IFrameStartListener
{
    private Group<Destroy> _g = new();

    public void OnFrameStart(IContext context)
    {
        _g.Refresh(context);

        foreach (var id in _g) {
            if (context.TryGet<LifetimeTokenSource>(id, out var tokenSource)) {
                tokenSource.Value.Cancel();
            }
            context.Clear(id);
        }
    }
}