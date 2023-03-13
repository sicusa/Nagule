namespace Nagule;

public interface IDeferrableCommand : ICommand
{
    bool ShouldExecute(ICommandHost host);
}