namespace Nagule;

using System.Collections.Concurrent;

public abstract class Command<TCommand> : ICommand
    where TCommand : ICommand, new()
{
    public virtual Guid? Id { get; } = null;
    public virtual int Priority { get; } = 0;

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

    public abstract void Execute(ICommandContext context);

    public virtual void Merge(ICommand other) {}
}

public abstract class Command<TCommand, TCommandTarget>
    : Command<TCommand>, ICommand<TCommandTarget>
    where TCommand : ICommand, new()
    where TCommandTarget : ICommandTarget
{
}

public static class Command
{
    public static Comparison<ICommand> ComparePriority { get; }
        = (c1, c2) => c1.Priority.CompareTo(c2.Priority);

    public static Comparison<(int, ICommand)> IndexedComparePriority { get; }
        = (c1, c2) => {
            var c = c1.Item2.Priority.CompareTo(c2.Item2.Priority);
            return c == 0 ? c1.Item1.CompareTo(c2.Item1) : c;
        };
}