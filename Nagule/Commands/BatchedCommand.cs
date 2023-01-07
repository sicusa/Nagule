namespace Nagule;

public class BatchedCommand : Command<BatchedCommand>
{
    public readonly List<ICommand> Commands = new();

    public override void Execute(IContext context)
    {
        throw new NotImplementedException();
    }

    public override void Dispose()
    {
        base.Dispose();
        Commands.Clear();
    }
}