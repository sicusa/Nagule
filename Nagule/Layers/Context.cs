namespace Nagule;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Aeco;
using Aeco.Local;
using Aeco.Reactive;

public class Context : CompositeLayer, IContext
{
    private class CommandTarget
    {
        public BlockingCollection<ICommand> Collection { get; } = new();

        private BatchedCommand _batch = BatchedCommand.Create();

        public void AddBatchedCommand(ICommand command)
        {
            lock (_batch) {
                _batch.Commands.Add(command);
            }
        }

        public void SubmitBatch()
        {
            BatchedCommand batch;

            lock (_batch) {
                batch = _batch;
                _batch = BatchedCommand.Create();
            }

            if (batch.Commands.Count != 0) {
                Collection.Add(batch);
            }
        }
    }

    public IDynamicCompositeLayer<IComponent> DynamicLayers { get; }
        = new DynamicCompositeLayer<IComponent>();
    
    public bool Running { get; protected set; }
    public float Time { get; protected set; }
    public float DeltaTime { get; protected set; }
    public long UpdateFrame { get; protected set; }

    public float RenderTime { get; protected set; }
    public float RenderDeltaTime { get; protected set; }
    public long RenderFrame { get; protected set; }

    private ConcurrentDictionary<Type, object> _listeners = new();
    private ConcurrentBag<Action<ILayer<IComponent>, bool>> _listenerHandlers = new();
    
    private ConcurrentDictionary<Type, CommandTarget> _commandTargets = new();

    public Context(params ILayer<IComponent>[] sublayers)
    {
        var eventStorage = new PolyTagStorage<IReactiveEvent>();
        var anyEventStorage = new PolySingletonStorage<IAnyReactiveEvent>();

        InternalAddSublayers(
            new DestroyedObjectCleaner(),
            new UnusedResourceDestroyer(),
            
            new NameRegisterer(),
            new TransformUpdator(),
            DynamicLayers);

        InternalAddSublayers(sublayers);

        InternalAddSublayers(
            new AutoClearer(anyEventStorage),
            new AutoClearer(eventStorage),

            anyEventStorage,
            eventStorage,

            new ReactiveCompositeLayer(
                new PolySingletonStorage<IReactiveSingletonComponent>(),
                new PolyTagStorage<IReactiveTagComponent>(),
                new PolyDenseStorage<IReactiveComponent>()) {
                EventDataLayer = eventStorage,
                AnyEventDataLayer = anyEventStorage
            },

            new PolySingletonStorage<ISingletonComponent>(),
            new PolyTagStorage<ITagComponent>(),
            new PolyDenseStorage<IPooledComponent>()
        );

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

    public virtual void Load()
    {
        Running = true;

        foreach (var listener in GetSublayersRecursively<ILoadListener>()) {
            try {
                listener.OnLoad(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke ILoadListener method for {listener}: " + e);
            }
        }

        InvokeFrameStartListeners();
        InvokeInternalFrameListeners();
    }

    public virtual void Unload()
    {
        if (!Running) { return; }
        Running = false;

        foreach (var listener in GetSublayersRecursively<IUnloadListener>()) {
            try {
                listener.OnUnload(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUnloadListener method for {listener}: " + e);
            }
        }
    }

    public virtual void Update(float deltaTime)
    {
        ++UpdateFrame;
        Time += deltaTime;
        DeltaTime = deltaTime;

        SubmitBatchedCommands();
        InvokeFrameStartListeners();

        foreach (var listener in GetListeners<IUpdateListener>()) {
            try {
                listener.OnUpdate(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUpdateListener method for {listener}: " + e);
            }
        }

        InvokeInternalFrameListeners();
    }

    private void InvokeFrameStartListeners()
    {
        foreach (var listener in GetListeners<IFrameStartListener>()) {
            try {
                listener.OnFrameStart(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IFrameStartListener method for {listener}: " + e);
            }
        }
    }

    private void InvokeInternalFrameListeners()
    {
        foreach (var listener in GetListeners<IResourceUpdateListener>()) {
            try {
                listener.OnResourceUpdate(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IResourceUpdateListener method for {listener}: " + e);
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

    public virtual void Render(float deltaTime)
    {
        ++RenderFrame;
        RenderTime += deltaTime;
        RenderDeltaTime = deltaTime;

        foreach (var listener in GetListeners<IRenderListener>()) {
            try {
                listener.OnRender(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IRenderListener method for {listener}: " + e);
            }
        }
    }

    public ReadOnlySpan<TListener> GetListeners<TListener>()
    {
        if (_listeners.TryGetValue(typeof(TListener), out var raw)) {
            return CollectionsMarshal.AsSpan((List<TListener>)raw);
        }

        var list = (List<TListener>)_listeners.AddOrUpdate(typeof(TListener),
            _ => {
                var list = new List<TListener>(GetSublayersRecursively<TListener>());
                _listenerHandlers.Add((layer, addOrRemove) => {
                    if (layer is TListener listener) {
                        if (addOrRemove) {
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

    public virtual void SubmitBatchedCommands()
    {
        foreach (var target in _commandTargets.Values) {
            target.SubmitBatch();
        }
    }

    public void SendCommandBatched<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().AddBatchedCommand(command);

    public void SendCommand<TTarget>(ICommand command)
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.Add(command);

    public bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command)
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.TryTake(out command);

    public ICommand WaitCommand<TTarget>()
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.Take();

    public IEnumerable<ICommand> ConsumeCommands<TTarget>()
        where TTarget : ICommandTarget
        => GetCommandTarget<TTarget>().Collection.GetConsumingEnumerable();

    private CommandTarget GetCommandTarget<TTarget>()
    {
        var type = typeof(TTarget);
        if (!_commandTargets.TryGetValue(type, out var target)) {
            target = _commandTargets.AddOrUpdate(
                type, _ => new(), (_, commands) => commands);
        }
        return target;
    }
}