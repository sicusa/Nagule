namespace Nagule;

public interface ICommand : IDisposable
{
    void Execute(IContext context);
}

public static class CommandExtensions
{
    public static void SafeExecuteAndDispose(this ICommand command, IContext context)
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
}