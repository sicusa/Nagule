namespace Nagule;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Aeco;
using Aeco.Local;

public class CommandContext : CompositeLayer, ICommandHost
{
    private ICommandBus _commandBus;

    public CommandContext(ICommandBus commandBus, params ILayer<IComponent>[] sublayers)
    {
        _commandBus = commandBus;

        InternalAddSublayers(sublayers);
        InternalAddSublayers(
            new PolySingletonStorage<ISingletonComponent>(),
            new PolyDenseStorage<IPooledComponent>());
    }

    public void SendCommand<TTarget>(ICommand command) where TTarget : ICommandTarget
        => _commandBus.SendCommand<TTarget>(command);

    public void SendCommandBatched<TTarget>(ICommand command) where TTarget : ICommandTarget
        => _commandBus.SendCommandBatched<TTarget>(command);

    public bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command) where TTarget : ICommandTarget
        => _commandBus.TryGetCommand<TTarget>(out command);

    public ICommand WaitCommand<TTarget>() where TTarget : ICommandTarget
        => _commandBus.WaitCommand<TTarget>();

    public IEnumerable<ICommand> ConsumeCommands<TTarget>() where TTarget : ICommandTarget
        => _commandBus.ConsumeCommands<TTarget>();

    public void SubmitBatchedCommands()
        => _commandBus.SubmitBatchedCommands();
}