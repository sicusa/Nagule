namespace Nagule;

public interface ICommand : IDisposable
{
    Guid? Id { get; }
    int Priority { get; }

    void Execute(ICommandContext context);
    void Merge(ICommand other);
}

public interface ICommand<TCommandTarget> : ICommand
    where TCommandTarget : ICommandTarget
{
}

public static class CommandExtensions
{
    public static void SafeExecuteAndDispose(this ICommand command, ICommandContext context)
    {
        try {
            command.Execute(context);
        }
        catch (Exception e) {
            Console.WriteLine($"Failed to execute command {command.GetType()}: " + e);
        }
        finally {
            command.Dispose();
        }
    }

    public static void SendCommand<TCommandTarget>(this ICommandBus commandBus, ICommand<TCommandTarget> command)
        where TCommandTarget : ICommandTarget
        => commandBus.SendCommand<TCommandTarget>(command);

    public static void SendCommandBatched<TCommandTarget>(this ICommandBus commandBus, ICommand<TCommandTarget> command)
        where TCommandTarget : ICommandTarget
        => commandBus.SendCommandBatched<TCommandTarget>(command);
}