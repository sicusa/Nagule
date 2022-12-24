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
            try {
                listener.OnUpdate(this, deltaTime);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUpdateListener method for {listener}: " + e);
            }
        }

        foreach (var listener in GetListeners<IEngineUpdateListener>()) {
            try {
                listener.OnEngineUpdate(this, deltaTime);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IEngineUpdateListener method for {listener}: " + e);
            }
        }

        foreach (var listener in GetListeners<ILateUpdateListener>()) {
            try {
                listener.OnLateUpdate(this, deltaTime);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke ILateUpdateListener method for {listener}: " + e);
            }
        }
    }

    public override void Render(float deltaTime)
    {
        ++RenderFrame;

        foreach (var listener in GetListeners<IRenderListener>()) {
            try {
                listener.OnRender(this, deltaTime);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IRenderListener method for {listener}: " + e);
            }
        }
        foreach (var listener in GetListeners<IRenderFinishedListener>()) {
            try {
                listener.OnRenderFinished(this, deltaTime);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IRenderFinishedListener method for {listener}: " + e);
            }
        }
    }
}