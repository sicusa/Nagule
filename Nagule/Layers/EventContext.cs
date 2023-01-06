namespace Nagule;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Aeco;

public abstract class EventContext : Context, IEventContext
{
    private ConcurrentDictionary<Type, object> _listeners = new();
    private ConcurrentBag<Action<ILayer<IComponent>, bool>> _listenerHandlers = new();

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

        var list = (List<TListener>)_listeners.AddOrUpdate(typeof(TListener),
            _ => {
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
                return list;
            },
            (_, list) => list);

        return CollectionsMarshal.AsSpan(list);
    }

    public override void StartFrame(float deltaTime)
    {
        ++Frame;
        Time += deltaTime;
        DeltaTime = deltaTime;

        foreach (var listener in GetListeners<IFrameStartListener>()) {
            try {
                listener.OnFrameStart(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IFrameStartListener method for {listener}: " + e);
            }
        }
    }

    public override void Update()
    {
        foreach (var listener in GetListeners<IUpdateListener>()) {
            try {
                listener.OnUpdate(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUpdateListener method for {listener}: " + e);
            }
        }

        foreach (var listener in GetListeners<IEngineUpdateListener>()) {
            try {
                listener.OnEngineUpdate(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IEngineUpdateListener method for {listener}: " + e);
            }
        }

        foreach (var listener in GetListeners<ILateUpdateListener>()) {
            try {
                listener.OnLateUpdate(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke ILateUpdateListener method for {listener}: " + e);
            }
        }
    }

    public override void Render()
    {
        foreach (var listener in GetListeners<IRenderBeginListener>()) {
            try {
                listener.OnRenderBegin(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IRenderFinishedListener method for {listener}: " + e);
            }
        }
        foreach (var listener in GetListeners<IRenderListener>()) {
            try {
                listener.OnRender(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IRenderListener method for {listener}: " + e);
            }
        }
    }
}