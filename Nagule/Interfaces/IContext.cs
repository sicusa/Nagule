namespace Nagule;

using Aeco;

public interface IContext
    : ICompositeDataLayer<IComponent, ILayer<IComponent>>, ICommandBus
{
    IDynamicCompositeLayer<IComponent> DynamicLayers { get; }

    bool Running { get; }
    float Time { get; }
    float DeltaTime { get; }
    long UpdateFrame { get; }

    float RenderTime { get; }
    float RenderDeltaTime { get; }
    long RenderFrame { get; }

    void Load();
    void Unload();
    void Update(float deltaTime);
    void Render(float deltaTime);

    ReadOnlySpan<TListener> GetListeners<TListener>();
}