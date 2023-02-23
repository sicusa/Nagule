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

    public abstract void Execute(ICommandHost host);

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
    
    private class DoCommand : Command<DoCommand>
    {
        public Guid? MergeId;
        public Action<ICommandHost>? Action;

        public override Guid? Id => MergeId;

        public override void Execute(ICommandHost host)
            => Action!.Invoke(host);
    }
    
    private class DeferrableDoCommand : Command<DeferrableDoCommand>, IDeferrableCommand
    {
        public Guid? MergeId;
        public Predicate<ICommandHost>? ShouldExecutePred;
        public Action<ICommandHost>? Action;

        public override Guid? Id => MergeId;

        public bool ShouldExecute(ICommandHost host)
            => ShouldExecutePred!.Invoke(host);

        public override void Execute(ICommandHost host)
            => Action!.Invoke(host);
    }
    
    public static ICommand Do(Action<ICommandHost> action)
    {
        var cmd = DoCommand.Create();
        cmd.Action = action;
        return cmd;
    }

    public static ICommand Do(Guid mergeId, Action<ICommandHost> action)
    {
        var cmd = DoCommand.Create();
        cmd.MergeId = mergeId;
        cmd.Action = action;
        return cmd;
    }

    public static ICommand DoDeferrable<T>(Predicate<ICommandHost> shouldExecute, Action<ICommandHost> action)
    {
        var cmd = DeferrableDoCommand.Create();
        cmd.ShouldExecutePred = shouldExecute;
        cmd.Action = action;
        return cmd;
    }

    public static ICommand DoDeferrable(Guid mergeId, Predicate<ICommandHost> shouldExecute, Action<ICommandHost> action)
    {
        var cmd = DeferrableDoCommand.Create();
        cmd.MergeId = mergeId;
        cmd.ShouldExecutePred = shouldExecute;
        cmd.Action = action;
        return cmd;
    }

    public static IDisposable SubscribeCommand<T, TTarget>(
        this IObservable<T> source, ICommandBus commandBus, Action<ICommandHost, T> action)
        where TTarget : ICommandTarget
        => source.Subscribe(value => 
            commandBus.SendCommand<TTarget>(
                Do(host => action(host, value))));

    public static IDisposable SubscribeCommand<T, TTarget>(
        this IObservable<T> source, ICommandBus commandBus, Action<T> action)
        where TTarget : ICommandTarget
        => source.Subscribe(value =>
            commandBus.SendCommand<TTarget>(
                Do(host => action(value))));
}