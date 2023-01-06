namespace Nagule;

using System.Collections.Concurrent;

public abstract class Command<TCommand> : ICommand
    where TCommand : ICommand, new()
{
    private static ConcurrentStack<ICommand> s_pool = new();

    public static TCommand Create()
    {
        if (s_pool.TryPop(out var command)) {
            return (TCommand)command;
        }
        return new TCommand();
    }

    public virtual void Dispose()
    {
        s_pool.Push(this);
    }

    public abstract void Execute(IContext context);
}