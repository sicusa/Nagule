namespace Nagule;

public abstract class SingletonCommand<TCommand> : ICommand
    where TCommand : ICommand, new()
{
    public virtual uint? Id { get; } = null;
    public virtual int Priority { get; } = 0;

    public static readonly TCommand Instance = new();

    protected SingletonCommand() {}

    public void Dispose() { }

    public virtual void Execute(ICommandHost host)
    {
        throw new NotImplementedException();
    }

    public virtual void Merge(ICommand other) {}
}

public abstract class SingletonCommand<TCommand, TCommandTarget>
    : SingletonCommand<TCommand>, ICommand<TCommandTarget>
    where TCommand : ICommand, new()
    where TCommandTarget : ICommandTarget
{
    protected SingletonCommand() {}
}

