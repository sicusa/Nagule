namespace Nagule;

public class BatchedCommand : Command<BatchedCommand>
{
    public readonly List<ICommand> Commands = new();

    public override void Execute(ICommandHost host)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        Commands.Clear();
        base.Dispose();
    }
}