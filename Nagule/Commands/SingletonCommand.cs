namespace Nagule;

public abstract class SingletonCommand<TCommand> : ICommand
    where TCommand : ICommand, new()
{
    public static readonly TCommand Instance = new();

    public void Execute(IContext context)
    {
        throw new NotImplementedException();
    }

    public void Dispose() { }
}
