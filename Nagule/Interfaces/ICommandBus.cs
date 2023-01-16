namespace Nagule;

using System.Diagnostics.CodeAnalysis;

public interface ICommandBus
{
    void SendCommand<TTarget>(ICommand command)
        where TTarget : ICommandTarget;
    void SendCommandBatched<TTarget>(ICommand command)
        where TTarget : ICommandTarget;

    bool TryGetCommand<TTarget>([MaybeNullWhen(false)] out ICommand command)
        where TTarget : ICommandTarget;
    ICommand WaitCommand<TTarget>()
        where TTarget : ICommandTarget;
    IEnumerable<ICommand> ConsumeCommands<TTarget>()
        where TTarget : ICommandTarget;

    void SubmitBatchedCommands();
}