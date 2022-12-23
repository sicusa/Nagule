namespace Nagule;

using System.Runtime.InteropServices;

using Aeco;

public abstract class EventContext : Context, IEventContext
{
    private Dictionary<Type, object> _listeners = new();
    private List<Action<ILayer<IComponent>, bool>> _listenerHandlers = new();

    public EventContext(params ILayer<IComponent>[] sublayers)
        : base(sublayers)
    {
    }

    public override void Load()
    {
        base.Load();

        DynamicLayers.SublayerAdded.Subscribe(layer => {
            foreach (var handler in _listenerHandlers) {
                handler(layer, true);
            }
        });

        DynamicLayers.SublayerRemoved.Subscribe(layer => {
            foreach (var handler in _listenerHandlers) {
                handler(layer, false);
            }
        });
    }

    public ReadOnlySpan<TListener> GetListeners<TListener>()
    {
        if (_listeners.TryGetValue(typeof(TListener), out var raw)) {
            return CollectionsMarshal.AsSpan((List<TListener>)raw);
        }

        var list = new List<TListener>(GetSublayersRecursively<TListener>());

        _listenerHandlers.Add((layer, shouldAdd) => {
            if (layer is TListener listener) {
                if (shouldAdd) {
                    list.Add(listener);
                }
                else {
                    list.Remove(listener);
                }
            }
        });

        _listeners.Add(typeof(TListener), list);
        return CollectionsMarshal.AsSpan(list);
    }

    public override void Update(float deltaTime)
    {
        Time += deltaTime;
        ++UpdateFrame;

        foreach (var listener in GetListeners<IUpdateListener>()) {
            listener.OnUpdate(this, deltaTime);
        }

        foreach (var listener in GetListeners<IEngineUpdateListener>()) {
            listener.OnEngineUpdate(this, deltaTime);
        }

        foreach (var listener in GetListeners<ILateUpdateListener>()) {
            listener.OnLateUpdate(this, deltaTime);
        }
    }

    public override void Render(float deltaTime)
    {
        ++RenderFrame;

        foreach (var listener in GetListeners<IRenderListener>()) {
            listener.OnRender(this, deltaTime);
        }
        foreach (var listener in GetListeners<IRenderFinishedListener>()) {
            listener.OnRenderFinished(this, deltaTime);
        }
    }
}