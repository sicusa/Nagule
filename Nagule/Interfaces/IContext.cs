namespace Nagule;

using Aeco;

public interface IContext
    : ICompositeDataLayer<IComponent, ILayer<IComponent>>, ICommandHost
{
    bool Running { get; }
    float Time { get; }
    float DeltaTime { get; }
    long Frame { get; }

    void Load();
    void Unload();
    void Update(float deltaTime);

    ReadOnlySpan<TListener> GetListeners<TListener>();
}