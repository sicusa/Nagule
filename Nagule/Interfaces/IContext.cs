namespace Nagule;

using Aeco;

public interface IContext : IDataLayer<IComponent>, ICompositeLayer<IComponent>
{
    IDynamicCompositeLayer<IComponent> DynamicLayers { get; }

    float Time { get; }
    long UpdateFrame { get; }
    long RenderFrame { get; }

    void Load();
    void Unload();
    void Update(float deltaTime);
    void Render(float deltaTime);
}