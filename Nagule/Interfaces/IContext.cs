namespace Nagule;

using Aeco;

public interface IContext : IDataLayer<IComponent>, ICompositeLayer<IComponent>
{
    IDynamicCompositeLayer<IComponent> DynamicLayers { get; }
    SortedSet<Guid> DirtyTransformIds { get; }

    float Time { get; }
    float DeltaTime { get; }
    long Frame { get; }

    void Load();
    void Unload();
    void StartFrame(float deltaTime);
    void Update();
    void Render();
}