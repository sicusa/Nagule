namespace Nagule;

using System.Diagnostics.CodeAnalysis;

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

    void SendCommand<TTarget>(ICommand command)
        where TTarget : ICommandTarget;
    bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command)
        where TTarget : ICommandTarget;
    ICommand WaitCommand<TTarget>()
        where TTarget : ICommandTarget;
    IEnumerable<ICommand> ConsumeCommands<TTarget>()
        where TTarget : ICommandTarget;
}