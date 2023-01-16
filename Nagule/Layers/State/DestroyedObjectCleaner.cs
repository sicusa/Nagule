namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class DestroyedObjectCleaner : Layer, ILateUpdateListener
{
    private Group<Destroy> _g = new();

    public void OnLateUpdate(IContext context)
    {
        foreach (var id in _g.Query(context)) {
            if (context.TryGet<LifetimeTokenSource>(id, out var tokenSource)) {
                tokenSource.Value.Cancel();
            }
            context.Clear(id);
        }
        if (_g.Count > 0) { _g.Clear(); }
    }
}