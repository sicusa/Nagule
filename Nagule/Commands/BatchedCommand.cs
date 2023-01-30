namespace Nagule;

public class BatchedCommand : Command<BatchedCommand>
{
    public readonly List<ICommand> Commands = new();

    public override void Execute(ICommandContext context)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        Commands.Clear();
        base.Dispose();
    }
}