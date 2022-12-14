namespace Nagule;

public class AsynchronizedCommand : Command<AsynchronizedCommand>
{
    public ICommand? Inner;

    public override void Execute(IContext context)
    {
        throw new NotImplementedException();
    }
}

public static class AsynchronizedCommandExtensions
{
    public static void SendCommandAsync<TCommandTarget>(this IContext context, ICommand cmd)
        where TCommandTarget : ICommandTarget
    {
        var syncCmd = AsynchronizedCommand.Create();
        syncCmd.Inner = cmd;
        context.SendCommand<TCommandTarget>(syncCmd);
    }

    public static void SendCommandBatchedAsync<TCommandTarget>(this IContext context, ICommand cmd)
        where TCommandTarget : ICommandTarget
    {
        var syncCmd = AsynchronizedCommand.Create();
        syncCmd.Inner = cmd;
        context.SendCommandBatched<TCommandTarget>(syncCmd);
    }
}