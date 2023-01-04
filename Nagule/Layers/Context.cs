namespace Nagule;

using Aeco;
using Aeco.Local;
using Aeco.Reactive;

public class Context : CompositeLayer, IContext
{
    public IDynamicCompositeLayer<IComponent> DynamicLayers { get; }
        = new DynamicCompositeLayer<IComponent>();
    
    public SortedSet<Guid> DirtyTransformIds { get; } = new SortedSet<Guid>();
    
    public float Time { get; protected set; }
    public float DeltaTime { get; protected set; }
    public long Frame { get; protected set; }

    private bool _unloaded;

    public Context(params ILayer<IComponent>[] sublayers)
    {
        var eventDataLayer = new CompositeLayer(
            new PolySingletonStorage<IAnyReactiveEvent>(),
            new PolyPoolStorage<IReactiveEvent>());

        InternalAddSublayers(
            new UnusedResourceDestroyer(),
            new NameRegisterer(),
            new TransformUpdator(),
            DynamicLayers);

        InternalAddSublayers(sublayers);

        InternalAddSublayers(
            new DestroyedObjectCleaner(),
            new AutoClearCompositeLayer(eventDataLayer),

            new ReactiveCompositeLayer(
                eventDataLayer: eventDataLayer,
                new PolyPoolStorage<IReactiveComponent>(),
                new PolySingletonStorage<IReactiveSingletonComponent>()),
            new PolySingletonStorage<ISingletonComponent>(),
            new PolyPoolStorage<IPooledComponent>()
        );
    }

    public virtual void Load()
    {
        foreach (var listener in GetSublayersRecursively<ILoadListener>()) {
            try {
                listener.OnLoad(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke ILoadListener method for {listener}: " + e);
            }
        }
    }

    public virtual void Unload()
    {
        if (_unloaded) { return; }
        _unloaded = true;

        foreach (var listener in GetSublayersRecursively<IUnloadListener>()) {
            try {
                listener.OnUnload(this);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to invoke IUnloadListener method for {listener}: " + e);
            }
        }
    }

    public virtual void StartFrame(float deltaTime)
    {
        ++Frame;
        Time += deltaTime;
        DeltaTime = deltaTime;
    }

    public virtual void Update() {}
    public virtual void Render() { }
}