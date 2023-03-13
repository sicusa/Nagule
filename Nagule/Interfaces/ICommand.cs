namespace Nagule;

public interface ICommand : IDisposable
{
    uint? Id { get; }
    int Priority { get; }

    void Execute(ICommandHost host);
    void Merge(ICommand other);
}

public interface ICommand<TCommandTarget> : ICommand
    where TCommandTarget : ICommandTarget
{
}

public static class CommandExtensions
{
    public static void SafeExecuteAndDispose(this ICommand command, ICommandHost host)
    {
        try {
            command.Execute(host);
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