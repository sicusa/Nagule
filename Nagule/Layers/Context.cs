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
    
    public float Time { get; protected set; }
    public long UpdateFrame { get; protected set; }
    public long RenderFrame { get; protected set; }

    public Context(params ILayer<IComponent>[] sublayers)
    {
        var eventDataLayer = new PolyPoolStorage<IReactiveEvent>();

        InternalAddSublayer(DynamicLayers);
        InternalAddSublayers(sublayers);
        InternalAddSublayers(
            new AutoClearCompositeLayer(eventDataLayer),
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