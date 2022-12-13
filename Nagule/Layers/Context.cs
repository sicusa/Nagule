namespace Nagule;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using Aeco;
using Aeco.Local;
using Aeco.Reactive;

public class Context : CompositeLayer, IContext
{
    public IDynamicCompositeLayer<IComponent> DynamicLayers { get; }
        = new DynamicCompositeLayer<IComponent>();
    
    public SortedSet<Guid> DirtyTransformIds { get; } = new SortedSet<Guid>();
    
    public float Time { get; protected set; }
    public long UpdateFrame { get; protected set; }
    public long RenderFrame { get; protected set; }

    private bool _unloaded;

    public Context(params ILayer<IComponent>[] sublayers)
    {
        var eventDataLayer = new CompositeLayer(
            new PolySingletonStorage<IAnyReactiveEvent>(),
            new PolyPoolStorage<IReactiveEvent>());

        InternalAddSublayers(
            new AutoClearCompositeLayer(eventDataLayer),
            new UnusedResourceDestroyer(),
            new NameRegisterer(),
            new TransformUpdator(),
            DynamicLayers);

        InternalAddSublayers(sublayers);

        InternalAddSublayers(
            new ReactiveCompositeLayer(
                eventDataLayer: eventDataLayer,
                new PolyPoolStorage<IReactiveComponent>()),
            new PolySingletonStorage<ISingletonComponent>(),
            new PolyPoolStorage<IPooledComponent>()
        );
    }

    public virtual void Load()
    {
        foreach (var listener in GetSublayersRecursively<ILoadListener>()) {
            listener.OnLoad(this);
        }
    }

    public virtual void Unload()
    {
        if (_unloaded) { return; }
        _unloaded = true;

        foreach (var listener in GetSublayersRecursively<IUnloadListener>()) {
            listener.OnUnload(this);
        }
    }

    public virtual void Update(float deltaTime)
    {
        Time += deltaTime;
        ++UpdateFrame;
    }

    public virtual void Render(float deltaTime)
    {
        ++RenderFrame;
    }
}