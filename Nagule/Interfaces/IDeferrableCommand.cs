namespace Nagule;

public interface IDeferrableCommand : ICommand
{
    public bool ShouldExecute(ICommandHost host);
}